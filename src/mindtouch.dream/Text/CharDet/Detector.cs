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

using MindTouch.Text.CharDet.Statistics;
using MindTouch.Text.CharDet.Verifiers;

namespace MindTouch.Text.CharDet {
    public class Detector : ICharsetDetector {

        //--- Constants ---
        public const int ALL = 0;
        public const int CHINESE = 2;
        public const int JAPANESE = 1;
        public const int SIMPLIFIED_CHINESE = 3;
        public const int TRADITIONAL_CHINESE = 4;
        public const int KOREAN = 5;
        private const int MAX_VERIFIERS = 16;
        private const int EIDXSFT4BITS = 3;
        private const int ESFTMSK4BITS = 7;
        private const int EBITSFT4BITS = 2;
        private const int EUNITMSK4BITS = 0x0000000F;

        //--- Fields ---
        protected int mClassItems;
        protected bool mClassRunSampler;
        protected bool mDone;
        protected int[] mItemIdx = new int[MAX_VERIFIERS];
        protected int mItems;
        protected bool mRunSampler;
        protected EUCSampler mSampler = new EUCSampler();
        protected byte[] mState = new byte[MAX_VERIFIERS];
        protected AStatistics[] mStatisticsData;
        protected AVerifier[] mVerifier;
        private ICharsetDetectionObserver mObserver;

        //--- Constructors ---
        protected Detector() : this(ALL) { }

        protected Detector(int langFlag) {
            initVerifiers(langFlag);
            Reset();
        }

        //--- Methods ---
        public void Reset() {
            mRunSampler = mClassRunSampler;
            mDone = false;
            mItems = mClassItems;

            for(int i = 0; i < mItems; i++) {
                mState[i] = 0;
                mItemIdx[i] = i;
            }

            mSampler.Reset();
        }

        protected void initVerifiers(int currVerSet) {
            mVerifier = null;
            mStatisticsData = null;
            switch(currVerSet) {
            case TRADITIONAL_CHINESE:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierBIG5(), 
                                               new VerifierISO2022CN(), 
                                               new VerifierEUCTW(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                mStatisticsData = new AStatistics[] {
                                                        null, 
                                                        new StatisticsBig5(), 
                                                        null, 
                                                        new StatisticsEUCTW(), 
                                                        null, 
                                                        null, 
                                                        null
                                                    };
                break;
            case KOREAN:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierEUCKR(), 
                                               new VerifierISO2022KR(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                break;
            case SIMPLIFIED_CHINESE:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierGB2312(), 
                                               new VerifierGB18030(), 
                                               new VerifierISO2022CN(), 
                                               new VerifierHZ(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                break;
            case JAPANESE:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierSJIS(), 
                                               new VerifierEUCJP(), 
                                               new VerifierISO2022JP(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                break;
            case CHINESE:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierGB2312(), 
                                               new VerifierGB18030(), 
                                               new VerifierBIG5(), 
                                               new VerifierISO2022CN(), 
                                               new VerifierHZ(), 
                                               new VerifierEUCTW(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                mStatisticsData = new AStatistics[] {
                                                        null, 
                                                        new StatisticsGB2312(), 
                                                        null, 
                                                        new StatisticsBig5(), 
                                                        null, 
                                                        null, 
                                                        new StatisticsEUCTW(), 
                                                        null, 
                                                        null, 
                                                        null
                                                    };
                break;
            case ALL:
            default:
                mVerifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierSJIS(), 
                                               new VerifierEUCJP(),
                                               new VerifierISO2022JP(), 
                                               new VerifierEUCKR(), 
                                               new VerifierISO2022KR(), 
                                               new VerifierBIG5(), 
                                               new VerifierEUCTW(), 
                                               new VerifierGB2312(), 
                                               new VerifierGB18030(), 
                                               new VerifierISO2022CN(), 
                                               new VerifierHZ(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                mStatisticsData = new AStatistics[] {
                                                        null, 
                                                        null, 
                                                        new StatisticsEUCJP(), 
                                                        null, 
                                                        new StatisticsEUCKR(), 
                                                        null, 
                                                        new StatisticsBig5(), 
                                                        new StatisticsEUCTW(), 
                                                        new StatisticsGB2312(), 
                                                        null, 
                                                        null, 
                                                        null, 
                                                        null, 
                                                        null, 
                                                        null
                                                    };
                break;
            }
            mClassRunSampler = (mStatisticsData != null);
            mClassItems = mVerifier.Length;
        }

        public void Report(string charset) {
            if(mObserver != null) {
                mObserver.Notify(charset);
            }
        }

        public bool HandleData(byte[] aBuf, int len) {
            int i;

            for(i = 0; i < len; i++) {
                byte b = aBuf[i];

                int j;
                for(j = 0; j < mItems;) {
                    byte st = getNextState(mVerifier[mItemIdx[j]], b, mState[j]);
                    //if (st != 0)
                    //System.out.println( "state(0x" + Integer.toHexString(0xFF&b) +") =>"+ Integer.toHexString(st&0xFF)+ " " + mVerifier[mItemIdx[j]].CharSet);

                    if(st == AVerifier.EItsMe) {
                        //System.out.println( "EItsMe(0x" + Integer.toHexString(0xFF&b) +") =>"+ mVerifier[mItemIdx[j]].CharSet);

                        Report(mVerifier[mItemIdx[j]].CharSet);
                        mDone = true;
                        return mDone;
                    }
                    if(st == AVerifier.EError) {
                        //System.out.println( "eNotMe(0x" + Integer.toHexString(0xFF&b) +") =>"+ mVerifier[mItemIdx[j]].CharSet);
                        mItems--;
                        if(j < mItems) {
                            mItemIdx[j] = mItemIdx[mItems];
                            mState[j] = mState[mItems];
                        }
                    } else {
                        mState[j++] = st;
                    }
                }

                if(mItems <= 1) {
                    if(1 == mItems) {
                        Report(mVerifier[mItemIdx[0]].CharSet);
                    }
                    mDone = true;
                    return mDone;
                }

                int nonUCS2Num = 0;
                int nonUCS2Idx = 0;

                for(j = 0; j < mItems; j++) {
                    if((!(mVerifier[mItemIdx[j]].isUCS2())) && (!(mVerifier[mItemIdx[j]].isUCS2()))) {
                        nonUCS2Num++;
                        nonUCS2Idx = j;
                    }
                }

                if(1 == nonUCS2Num) {
                    Report(mVerifier[mItemIdx[nonUCS2Idx]].CharSet);
                    mDone = true;
                    return mDone;
                }
            } // End of for( i=0; i < len ...

            if(mRunSampler) {
                Sample(aBuf, len);
            }

            return mDone;
        }

        public void DataEnd() {
            if(mDone) {
                return;
            }
            if(mItems == 2) {
                if(mVerifier[mItemIdx[0]].CharSet == "GB18030") {
                    Report(mVerifier[mItemIdx[1]].CharSet);
                    mDone = true;
                } else if(mVerifier[mItemIdx[1]].CharSet == "GB18030") {
                    Report(mVerifier[mItemIdx[0]].CharSet);
                    mDone = true;
                }
            }
            if(mRunSampler) {
                Sample(null, 0, true);
            }
        }

        public void Sample(byte[] aBuf, int aLen) {
            Sample(aBuf, aLen, false);
        }

        public void Sample(byte[] aBuf, int aLen, bool aLastChance) {
            int possibleCandidateNum = 0;
            int j;
            int eucNum = 0;
            for(j = 0; j < mItems; j++) {
                if(null != mStatisticsData[mItemIdx[j]]) {
                    eucNum++;
                }
                if((!mVerifier[mItemIdx[j]].isUCS2()) && (mVerifier[mItemIdx[j]].CharSet != "GB18030")) {
                    possibleCandidateNum++;
                }
            }
            mRunSampler = (eucNum > 1);
            if(mRunSampler) {
                mRunSampler = mSampler.Sample(aBuf, aLen);
                if(((aLastChance && mSampler.GetSomeData()) || mSampler.EnoughData()) && (eucNum == possibleCandidateNum)) {
                    mSampler.CalFreq();

                    int bestIdx = -1;
                    int eucCnt = 0;
                    float bestScore = 0.0f;
                    for(j = 0; j < mItems; j++) {
                        if((null != mStatisticsData[mItemIdx[j]]) && (mVerifier[mItemIdx[j]].CharSet != "Big5")) {
                            float score = mSampler.GetScore(mStatisticsData[mItemIdx[j]].FirstByteFreq, mStatisticsData[mItemIdx[j]].FirstByteWeight, mStatisticsData[mItemIdx[j]].SecondByteFreq, mStatisticsData[mItemIdx[j]].SecondByteWeight);
                            //System.out.println("FequencyScore("+mVerifier[mItemIdx[j]].CharSet+")= "+ score);
                            if((0 == eucCnt++) || (bestScore > score)) {
                                bestScore = score;
                                bestIdx = j;
                            } // if(( 0 == eucCnt++) || (bestScore > score )) 
                        } // if(null != ...)
                    } // for
                    if(bestIdx >= 0) {
                        Report(mVerifier[mItemIdx[bestIdx]].CharSet);
                        mDone = true;
                    }
                } // if (eucNum == possibleCandidateNum)
            } // if(mRunSampler)
        }

        public string[] GetProbableCharsets() {
            if(mItems <= 0) {
                var nomatch = new string[1];
                nomatch[0] = "nomatch";
                return nomatch;
            }
            var ret = new string[mItems];
            for(int i = 0; i < mItems; i++) {
                ret[i] = mVerifier[mItemIdx[i]].CharSet;
            }
            return ret;
        }

        public static byte getNextState(AVerifier v, byte b, byte s) {
            return (byte)(0xFF & (((v.States[(((s * v.StFactor + (((v.Cclass[((b & 0xFF) >> EIDXSFT4BITS)]) >> ((b & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS)) & 0xFF) >> EIDXSFT4BITS)]) >> ((((s * v.StFactor + (((v.Cclass[((b & 0xFF) >> EIDXSFT4BITS)]) >> ((b & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS)) & 0xFF) & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS));
        }

        public void Init(ICharsetDetectionObserver aObserver) {
            mObserver = aObserver;
            return;
        }

        public bool DoIt(byte[] buffer, int aLen, bool oDontFeedMe) {
            if(buffer == null || oDontFeedMe) {
                return false;
            }
            HandleData(buffer, aLen);
            return mDone;
        }

        public void Done() {
            DataEnd();
            return;
        }

        public bool IsAscii(byte[] aBuf, int aLen) {
            for(int i = 0; i < aLen; i++) {
                if((0x0080 & aBuf[i]) != 0) {
                    return false;
                }
            }
            return true;
        }
    }
}