/* --------------------------------------------------------------------------
 *
 * License
 *
 * The contents of this file are subject to the Jabber Open Source License
 * Version 1.0 (the "License").  You may not copy or use this file, in either
 * source code or executable form, except in compliance with the License.  You
 * may obtain a copy of the License at http://www.jabber.com/license/ or at
 * http://www.opensource.org/.  
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied.  See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * Copyrights
 * 
 * Portions created by or assigned to Cursive Systems, Inc. are 
 * Copyright (c) 2002 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at http://www.cursive.net/.
 *
 * Portions Copyright (c) 2003 Joe Hildebrand.
 * 
 * Acknowledgements
 * 
 * Special thanks to the Jabber Open Source Contributors for their
 * suggestions and support of Jabber.
 * 
 * --------------------------------------------------------------------------*/

/*
 * Assumption: UCS2.  The astral planes don't exist.  At least according to windows?
 * 
 * Look over here.  Something shiny!
 */

using System;
using System.Text;
using stringprep.unicode;

namespace stringprep.steps
{
    /// <summary>
    /// Perform Unicode Normalization Form KC.
    /// </summary>
    public class NFKCStep : ProfileStep
    {
        /// <summary>
        /// Create an NFKC step.
        /// </summary>
        public NFKCStep() : base("NFKC", ProfileFlags.NO_NFKC, false)
        {
        }

        /// <summary>
        /// Perform NFKC.  General overview: Decompose, Reorder, Compose
        /// </summary>
        /// <param name="result"></param>
        /// <param name="flags"></param>
        public override void Prepare(StringBuilder result, ProfileFlags flags)
        {
            if (IsBitSet(flags))
                return;

            // From Unicode TR15:
            // R1. Normalization Form C
            // The Normalization Form C for a string S is obtained by applying the following process, 
            // or any other process that leads to the same result:
            //
            // 1) Generate the canonical decomposition for the source string S according to the 
            // decomposition mappings in the latest supported version of the Unicode Character Database. 
            //
            // 2) Iterate through each character C in that decomposition, from first to last. 
            // If C is not blocked from the last starter L, and it can be primary combined with L, 
            // then replace L by the composite L-C, and remove C. 

            int decomp;
            int offset;
            int last_start = 0;
            int old_len;
            char[] insert = new char[16];
            int j;

            // Decompose
            for (int i=0; i< result.Length; i++)
            {
                char c = result[i];

                old_len = result.Length;
                decomp = Decompose.Find(c);
                if (decomp >= 0)
                {
                    // 0, 0 terminates.
                    for (offset = decomp, j=0; Decompose.More(offset); offset += 2, j++)
                    {
                        insert[j] = Decompose.Expand(offset);
                    }

                    switch (j)
                    {
                    case 0:
                         // there were no characters in the replacement.  just remove the existing char.
                        result.Remove(i, 1);
                        i--;
                        break;
                    case 1:
                        // exactly one.  Just replace that char.
                        result[i] = insert[0];
                        break;
                    default:
                        // more than one.  replace the 0th char, and insert the rest.
                        // yes, it would have been cool to have a function like this in StringBuilder.
                        result[i] = insert[0];
                        result.Insert(i+1, insert, 1, j-1);
                        i += (j-1);
                        break;
                    }
                }

                if ((result.Length > 0) && (old_len < result.Length))
                {
                    if (Decompose.CombiningClass(result[old_len]) == 0)
                    { 
                        Decompose.CanonicalOrdering(result, last_start, result.Length - last_start);
                        last_start = old_len;
                    }
                }
            }

            // stuff left at the end?
            if (result.Length > 0)
            {
                Decompose.CanonicalOrdering(result, last_start, result.Length - last_start);
            }

            // All decomposed and reordered.
            // Combine all combinable characters.
            if (result.Length > 0)
            {
                int cc;
                int last_cc = 0;
                char c;
                last_start = 0;

                for (int i=0; i<result.Length; i++)
                {
                    cc = Decompose.CombiningClass(result[i]);
                    if ((i > 0) &&
                        ((last_cc == 0) || (last_cc != cc)) &&
                        Compose.Combine(result[last_start], result[i], out c))
                    {
                        result[last_start] = c;
                        result.Remove(i, 1);
                        i--;

                        if (i == last_start)
                            last_cc = 0;
                        else
                            last_cc = Decompose.CombiningClass(result[i - 1]);

                        continue;
                    }

                    if (cc == 0)
                        last_start = i;

                    last_cc = cc;
                }
            }        
        }
      
    }
}