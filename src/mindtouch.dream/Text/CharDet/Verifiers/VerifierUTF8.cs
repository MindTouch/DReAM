/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is mozilla.org code.
 *
 * The Initial Developer of the Original Code is
 * Netscape Communications Corporation.
 * Portions created by the Initial Developer are Copyright (C) 1998
 * the Initial Developer. All Rights Reserved.
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either of the GNU General Public License Version 2 or later (the "GPL"),
 * or the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */

namespace MindTouch.Text.CharDet.Verifiers {
    internal class VerifierUTF8  : AVerifier {

        //--- Constructors ---
        public VerifierUTF8() {
            Cclass = new int[256/8] ;
            Cclass[0] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[1] = ((int)(((  ((int)(((  ((int)((( 0) << 4) | (0)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[2] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[3] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((0) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[4] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[5] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[6] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[7] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[8] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[9] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[10] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[11] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[12] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[13] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[14] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[15] = ((int)(((  ((int)(((  ((int)((( 1) << 4) | (1)))  ) << 8) | (((int)(((1) << 4) | ( 1))) ))) ) << 16) | (  ((int)(((  ((int)(((1) << 4) | (1))) ) << 8) | (   ((int)(((1) << 4) | (1))) )))))) ;
            Cclass[16] = ((int)(((  ((int)(((  ((int)((( 3) << 4) | (3)))  ) << 8) | (((int)(((3) << 4) | ( 3))) ))) ) << 16) | (  ((int)(((  ((int)(((2) << 4) | (2))) ) << 8) | (   ((int)(((2) << 4) | (2))) )))))) ;
            Cclass[17] = ((int)(((  ((int)(((  ((int)((( 4) << 4) | (4)))  ) << 8) | (((int)(((4) << 4) | ( 4))) ))) ) << 16) | (  ((int)(((  ((int)(((4) << 4) | (4))) ) << 8) | (   ((int)(((4) << 4) | (4))) )))))) ;
            Cclass[18] = ((int)(((  ((int)(((  ((int)((( 4) << 4) | (4)))  ) << 8) | (((int)(((4) << 4) | ( 4))) ))) ) << 16) | (  ((int)(((  ((int)(((4) << 4) | (4))) ) << 8) | (   ((int)(((4) << 4) | (4))) )))))) ;
            Cclass[19] = ((int)(((  ((int)(((  ((int)((( 4) << 4) | (4)))  ) << 8) | (((int)(((4) << 4) | ( 4))) ))) ) << 16) | (  ((int)(((  ((int)(((4) << 4) | (4))) ) << 8) | (   ((int)(((4) << 4) | (4))) )))))) ;
            Cclass[20] = ((int)(((  ((int)(((  ((int)((( 5) << 4) | (5)))  ) << 8) | (((int)(((5) << 4) | ( 5))) ))) ) << 16) | (  ((int)(((  ((int)(((5) << 4) | (5))) ) << 8) | (   ((int)(((5) << 4) | (5))) )))))) ;
            Cclass[21] = ((int)(((  ((int)(((  ((int)((( 5) << 4) | (5)))  ) << 8) | (((int)(((5) << 4) | ( 5))) ))) ) << 16) | (  ((int)(((  ((int)(((5) << 4) | (5))) ) << 8) | (   ((int)(((5) << 4) | (5))) )))))) ;
            Cclass[22] = ((int)(((  ((int)(((  ((int)((( 5) << 4) | (5)))  ) << 8) | (((int)(((5) << 4) | ( 5))) ))) ) << 16) | (  ((int)(((  ((int)(((5) << 4) | (5))) ) << 8) | (   ((int)(((5) << 4) | (5))) )))))) ;
            Cclass[23] = ((int)(((  ((int)(((  ((int)((( 5) << 4) | (5)))  ) << 8) | (((int)(((5) << 4) | ( 5))) ))) ) << 16) | (  ((int)(((  ((int)(((5) << 4) | (5))) ) << 8) | (   ((int)(((5) << 4) | (5))) )))))) ;
            Cclass[24] = ((int)(((  ((int)(((  ((int)((( 6) << 4) | (6)))  ) << 8) | (((int)(((6) << 4) | ( 6))) ))) ) << 16) | (  ((int)(((  ((int)(((6) << 4) | (6))) ) << 8) | (   ((int)(((0) << 4) | (0))) )))))) ;
            Cclass[25] = ((int)(((  ((int)(((  ((int)((( 6) << 4) | (6)))  ) << 8) | (((int)(((6) << 4) | ( 6))) ))) ) << 16) | (  ((int)(((  ((int)(((6) << 4) | (6))) ) << 8) | (   ((int)(((6) << 4) | (6))) )))))) ;
            Cclass[26] = ((int)(((  ((int)(((  ((int)((( 6) << 4) | (6)))  ) << 8) | (((int)(((6) << 4) | ( 6))) ))) ) << 16) | (  ((int)(((  ((int)(((6) << 4) | (6))) ) << 8) | (   ((int)(((6) << 4) | (6))) )))))) ;
            Cclass[27] = ((int)(((  ((int)(((  ((int)((( 6) << 4) | (6)))  ) << 8) | (((int)(((6) << 4) | ( 6))) ))) ) << 16) | (  ((int)(((  ((int)(((6) << 4) | (6))) ) << 8) | (   ((int)(((6) << 4) | (6))) )))))) ;
            Cclass[28] = ((int)(((  ((int)(((  ((int)((( 8) << 4) | (8)))  ) << 8) | (((int)(((8) << 4) | ( 8))) ))) ) << 16) | (  ((int)(((  ((int)(((8) << 4) | (8))) ) << 8) | (   ((int)(((8) << 4) | (7))) )))))) ;
            Cclass[29] = ((int)(((  ((int)(((  ((int)((( 8) << 4) | (8)))  ) << 8) | (((int)(((9) << 4) | ( 8))) ))) ) << 16) | (  ((int)(((  ((int)(((8) << 4) | (8))) ) << 8) | (   ((int)(((8) << 4) | (8))) )))))) ;
            Cclass[30] = ((int)(((  ((int)(((  ((int)((( 11) << 4) | (11)))  ) << 8) | (((int)(((11) << 4) | ( 11))) ))) ) << 16) | (  ((int)(((  ((int)(((11) << 4) | (11))) ) << 8) | (   ((int)(((11) << 4) | (10))) )))))) ;
            Cclass[31] = ((int)(((  ((int)(((  ((int)((( 0) << 4) | (0)))  ) << 8) | (((int)(((15) << 4) | ( 14))) ))) ) << 16) | (  ((int)(((  ((int)(((13) << 4) | (13))) ) << 8) | (   ((int)(((13) << 4) | (12))) )))))) ;
            States = new int[26] ;
            States[0] = ((int)(((  ((int)(((  ((int)(((      10) << 4) | (     12)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EStart) << 4) | (EError))) )))))) ;
            States[1] = ((int)(((  ((int)(((  ((int)(((      3) << 4) | (     4)))  ) << 8) | (((int)(((     5) << 4) | (      6))) ))) ) << 16) | (  ((int)(((  ((int)(((     7) << 4) | (     8))) ) << 8) | (   ((int)(((     11) << 4) | (     9))) )))))) ;
            States[2] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[3] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[4] = ((int)(((  ((int)(((  ((int)((( EItsMe) << 4) | (EItsMe)))  ) << 8) | (((int)(((EItsMe) << 4) | ( EItsMe))) ))) ) << 16) | (  ((int)(((  ((int)(((EItsMe) << 4) | (EItsMe))) ) << 8) | (   ((int)(((EItsMe) << 4) | (EItsMe))) )))))) ;
            States[5] = ((int)(((  ((int)(((  ((int)((( EItsMe) << 4) | (EItsMe)))  ) << 8) | (((int)(((EItsMe) << 4) | ( EItsMe))) ))) ) << 16) | (  ((int)(((  ((int)(((EItsMe) << 4) | (EItsMe))) ) << 8) | (   ((int)(((EItsMe) << 4) | (EItsMe))) )))))) ;
            States[6] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     5) << 4) | (      5))) ))) ) << 16) | (  ((int)(((  ((int)(((     5) << 4) | (     5))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[7] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[8] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     5) << 4) | (      5))) ))) ) << 16) | (  ((int)(((  ((int)(((     5) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[9] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[10] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     7) << 4) | (      7))) ))) ) << 16) | (  ((int)(((  ((int)(((     7) << 4) | (     7))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[11] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[12] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     7) << 4) | (      7))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[13] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[14] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     9) << 4) | (      9))) ))) ) << 16) | (  ((int)(((  ((int)(((     9) << 4) | (     9))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[15] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[16] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     9) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[17] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[18] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     12) << 4) | (      12))) ))) ) << 16) | (  ((int)(((  ((int)(((     12) << 4) | (     12))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[19] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[20] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((     12) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[21] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[22] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | (      12))) ))) ) << 16) | (  ((int)(((  ((int)(((     12) << 4) | (     12))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[23] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[24] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EStart) << 4) | ( EStart))) ))) ) << 16) | (  ((int)(((  ((int)(((EStart) << 4) | (EStart))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            States[25] = ((int)(((  ((int)(((  ((int)((( EError) << 4) | (EError)))  ) << 8) | (((int)(((EError) << 4) | ( EError))) ))) ) << 16) | (  ((int)(((  ((int)(((EError) << 4) | (EError))) ) << 8) | (   ((int)(((EError) << 4) | (EError))) )))))) ;
            CharSet =  "UTF-8";
            StFactor =  16;
            Priority = 10;
        }

        //--- Methods ---
        public override bool isUCS2() { return  false; }
    }
}