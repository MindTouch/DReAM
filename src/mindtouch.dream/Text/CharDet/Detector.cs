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

/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Text;
using MindTouch.Text.CharDet.Statistics;
using MindTouch.Text.CharDet.Verifiers;

namespace MindTouch.Text.CharDet {
    internal enum Language {
        ALL = 0,
        JAPANESE = 1,
        CHINESE = 2,
        SIMPLIFIED_CHINESE = 3,
        TRADITIONAL_CHINESE = 4,
        KOREAN = 5
    }

    internal class Detector {

        //--- Constants ---
        private const int EIDXSFT4BITS = 3;
        private const int ESFTMSK4BITS = 7;
        private const int EBITSFT4BITS = 2;
        private const int EUNITMSK4BITS = 0x0000000F;

        //--- Fields ---
        private int _items;
        private byte[] _state;
        private AStatistics[] _statisticsData;
        private AVerifier[] _verifier;
        private EUCSampler _sampler;

        //--- Constructors ---
        public Detector() : this(Language.ALL) { }

        public Detector(Language language) {
            InitVerifiers(language);
            Reset();
        }

        //--- Properties ---
        public Encoding Result { get; private set; }

        //--- Methods ---
        public void Reset() {
            Result = null;
            _items = _verifier.Length;
            _state = new byte[_items];
            if(_statisticsData != null) {
                _sampler = new EUCSampler();
                _sampler.Reset();
            }
        }

        public void HandleData(byte[] buffer, int length) {
            int i;
            for(i = 0; i < length; i++) {
                byte b = buffer[i];
                int j;
                for(j = 0; j < _items; ) {
                    byte st = getNextState(_verifier[j], b, _state[j]);
                    if(st == AVerifier.EItsMe) {

                        //System.out.println( "EItsMe(0x" + Integer.toHexString(0xFF&b) +") =>"+ _verifier[j].CharSet);
                        Report(_verifier[j]);
                        return;
                    }
                    if(st == AVerifier.EError) {
                        _items--;
                        if(j < _items) {
                            _verifier[j] = _verifier[_items];
                            _verifier[_items] = null;
                            _statisticsData[j] = _statisticsData[_items];
                            _statisticsData[_items] = null;
                            _state[j] = _state[_items];
                        } else {
                            _verifier[_items] = null;
                            _statisticsData[_items] = null;                            
                        }
                    } else {
                        _state[j++] = st;
                    }
                }
                if(_items <= 1) {
                    if(1 == _items) {
                        Report(_verifier[0]);
                    }
                    return;
                }
            } // End of for( i=0; i < length ...
            if(_sampler != null) {
                _sampler.Sample(buffer, length);
                if(_sampler.EnoughData()) {
                    UseSamplerGuess();
                }
            }
        }

        public void DataEnd() {
            if(Result != null) {
                return;
            }
            if(_items == 1) {
                Report(_verifier[0]);
            } else {
                if(_sampler != null) {
                    UseSamplerGuess();
                }
                if(Result == null) {
                    int bestIndex = -1;
                    int bestPriority = 0;
                    int bestCount = 0;
                    for(int i = 0; i < _items; ++i) {
                        if(_verifier[i].Priority > bestPriority) {
                            bestCount = 1;
                            bestPriority = _verifier[i].Priority;
                            bestIndex = i;
                        } else if(_verifier[i].Priority == bestPriority) {
                            ++bestCount;
                        }
                    }
                    if(bestCount == 1) {
                        Report(_verifier[bestIndex]);
                    }
                }
            }
        }

        private void UseSamplerGuess() {
            _sampler.CalFreq();
            int bestIdx = -1;
            float bestScore = float.MaxValue;
            for(int j = 0; j < _items; j++) {
                if(null != _statisticsData[j]) {
                    float score = _sampler.GetScore(_statisticsData[j].FirstByteFreq, _statisticsData[j].FirstByteWeight, _statisticsData[j].SecondByteFreq, _statisticsData[j].SecondByteWeight);
                    if(bestScore > score) {
                        bestScore = score;
                        bestIdx = j;
                    }
                }
            }
            if(bestIdx >= 0) {
                Report(_verifier[bestIdx]);
            }
        }

        private static byte getNextState(AVerifier v, byte b, byte s) {
            return (byte)(0xFF & (((v.States[(((s * v.StFactor + (((v.Cclass[((b & 0xFF) >> EIDXSFT4BITS)]) >> ((b & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS)) & 0xFF) >> EIDXSFT4BITS)]) >> ((((s * v.StFactor + (((v.Cclass[((b & 0xFF) >> EIDXSFT4BITS)]) >> ((b & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS)) & 0xFF) & ESFTMSK4BITS) << EBITSFT4BITS)) & EUNITMSK4BITS));
        }

        private void Report(AVerifier verifier) {
            try {
                Result = Encoding.GetEncoding(verifier.CharSet);
            } catch(ArgumentException) {
                Result = null;
            }
        }

        private void InitVerifiers(Language currVerSet) {
            _verifier = null;
            _statisticsData = null;
            switch(currVerSet) {
            case Language.TRADITIONAL_CHINESE:
                _verifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierBIG5(), 
                                               new VerifierISO2022CN(), 
                                               new VerifierEUCTW(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                _statisticsData = new AStatistics[] {
                                                        null, 
                                                        new StatisticsBig5(), 
                                                        null, 
                                                        new StatisticsEUCTW(), 
                                                        null, 
                                                        null, 
                                                        null
                                                    };
                break;
            case Language.KOREAN:
                _verifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierEUCKR(), 
                                               new VerifierISO2022KR(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                break;
            case Language.SIMPLIFIED_CHINESE:
                _verifier = new AVerifier[] {
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
            case Language.JAPANESE:
                _verifier = new AVerifier[] {
                                               new VerifierUTF8(), 
                                               new VerifierSJIS(), 
                                               new VerifierEUCJP(), 
                                               new VerifierISO2022JP(), 
                                               new VerifierCP1252(), 
                                               new VerifierUCS2BE(), 
                                               new VerifierUCS2LE()
                                           };
                break;
            case Language.CHINESE:
                _verifier = new AVerifier[] {
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
                _statisticsData = new AStatistics[] {
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
            case Language.ALL:
            default:
                _verifier = new AVerifier[] {
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
                _statisticsData = new AStatistics[] {
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
        }
    }
}