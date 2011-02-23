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

using System;

namespace MindTouch.Text.CharDet {
    internal class EUCSampler {

        //--- Constants ---
        private const int mThreshold = 200;

        //--- Fields ---
        private int mTotal;
        private int mState;
        private readonly int[] mFirstByteCnt = new int[94];
        private readonly int[] mSecondByteCnt = new int[94];
        private readonly float[] mFirstByteFreq = new float[94];
        private readonly float[] mSecondByteFreq = new float[94];

        //--- Constructors ---
        public EUCSampler() {
            Reset();
        }

        //--- Methods ---
        public void Reset() {
            mTotal = 0;
            mState = 0;
            for(int i = 0; i < 94; i++) {
                mFirstByteCnt[i] = mSecondByteCnt[i] = 0;
            }
        }

        public bool EnoughData() { return mTotal > mThreshold; }

        public bool GetSomeData() { return mTotal > 1; }

        public bool Sample(byte[] aIn, int aLen) {
            if(mState == 1) {
                return false;
            }
            int p = 0;
            for(int i = 0; (i < aLen) && (1 != mState); i++, p++) {
                switch(mState) {
                case 0:
                    if((aIn[p] & 0x0080) != 0) {
                        if((0xff == (0xff & aIn[p])) || (0xa1 > (0xff & aIn[p]))) {
                            mState = 1;
                        } else {
                            mTotal++;
                            mFirstByteCnt[(0xff & aIn[p]) - 0xa1]++;
                            mState = 2;
                        }
                    }
                    break;
                case 1:
                    break;
                case 2:
                    if((aIn[p] & 0x0080) != 0) {
                        if((0xff == (0xff & aIn[p]))
                           || (0xa1 > (0xff & aIn[p]))) {
                            mState = 1;
                        } else {
                            mTotal++;
                            mSecondByteCnt[(0xff & aIn[p]) - 0xa1]++;
                            mState = 0;
                        }
                    } else {
                        mState = 1;
                    }
                    break;
                default:
                    mState = 1;
                    break;
                }
            }
            return (1 != mState);
        }

        public void CalFreq() {
            for(int i = 0; i < 94; i++) {
                mFirstByteFreq[i] = mFirstByteCnt[i] / (float)mTotal;
                mSecondByteFreq[i] = mSecondByteCnt[i] / (float)mTotal;
            }
        }

        public float GetScore(float[] aFirstByteFreq, float aFirstByteWeight, float[] aSecondByteFreq, float aSecondByteWeight) {
            return aFirstByteWeight * GetScore(aFirstByteFreq, mFirstByteFreq) +
                   aSecondByteWeight * GetScore(aSecondByteFreq, mSecondByteFreq);
        }

        public float GetScore(float[] array1, float[] array2) {
            float sum = 0.0f;
            for(int i = 0; i < 94; i++) {
                float s = array1[i] - array2[i];
                sum += s * s;
            }
            return (float)Math.Sqrt(sum) / 94.0f;
        }
    }
}