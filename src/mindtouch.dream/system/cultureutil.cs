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

using System.Collections.Generic;
using System.Globalization;

namespace System {

    /// <summary>
    /// Static Utility class containing extension and helper methods for working with <see cref="CultureInfo"/>.
    /// </summary>
    public static class CultureUtil {

        //--- Class Fields ---
        private static readonly Dictionary<string, string> _neutralTofullCulture = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        //--- Class Contructor ---
        static CultureUtil() {
            _neutralTofullCulture["af"] = "af-za";
            _neutralTofullCulture["sq"] = "sq-al";
            _neutralTofullCulture["ar"] = "ar-sa";
            _neutralTofullCulture["hy"] = "hy-am";
            _neutralTofullCulture["az"] = "az-latn-az";
            _neutralTofullCulture["az-cyrl"] = "az-cyrl-az";
            _neutralTofullCulture["az-latn"] = "az-latn-az";
            _neutralTofullCulture["eu"] = "eu-es";
            _neutralTofullCulture["be"] = "be-by";
            _neutralTofullCulture["bg"] = "bg-bg";
            _neutralTofullCulture["ca"] = "ca-es";
            _neutralTofullCulture["zh"] = "zh-cn";
            _neutralTofullCulture["hr"] = "hr-hr";
            _neutralTofullCulture["cs"] = "cs-cz";
            _neutralTofullCulture["da"] = "da-dk";
            _neutralTofullCulture["dv"] = "dv-mv";
            _neutralTofullCulture["nl"] = "nl-nl";
            _neutralTofullCulture["en"] = "en-us";
            _neutralTofullCulture["et"] = "et-ee";
            _neutralTofullCulture["fo"] = "fo-fo";
            _neutralTofullCulture["fa"] = "fa-ir";
            _neutralTofullCulture["fi"] = "fi-fi";
            _neutralTofullCulture["fr"] = "fr-fr";
            _neutralTofullCulture["gl"] = "gl-es";
            _neutralTofullCulture["ka"] = "ka-ge";
            _neutralTofullCulture["de"] = "de-de";
            _neutralTofullCulture["el"] = "el-gr";
            _neutralTofullCulture["gu"] = "gu-in";
            _neutralTofullCulture["he"] = "he-il";
            _neutralTofullCulture["hi"] = "hi-in";
            _neutralTofullCulture["hu"] = "hu-hu";
            _neutralTofullCulture["is"] = "is-is";
            _neutralTofullCulture["id"] = "id-id";
            _neutralTofullCulture["it"] = "it-it";
            _neutralTofullCulture["ja"] = "ja-jp";
            _neutralTofullCulture["kn"] = "kn-in";
            _neutralTofullCulture["kk"] = "kk-kz";
            _neutralTofullCulture["kok"] = "kok-in";
            _neutralTofullCulture["ko"] = "ko-kr";
            _neutralTofullCulture["ky"] = "ky-kg";
            _neutralTofullCulture["lv"] = "lv-lv";
            _neutralTofullCulture["lt"] = "lt-lt";
            _neutralTofullCulture["mk"] = "mk-mk";
            _neutralTofullCulture["ms"] = "ms-my";
            _neutralTofullCulture["mr"] = "mr-in";
            _neutralTofullCulture["mn"] = "mn-mn";
            _neutralTofullCulture["nb"] = "nb-no";
            _neutralTofullCulture["nn"] = "nn-no";
            _neutralTofullCulture["pl"] = "pl-pl";
            _neutralTofullCulture["pt"] = "pt-pt";
            _neutralTofullCulture["pa"] = "pa-in";
            _neutralTofullCulture["ro"] = "ro-ro";
            _neutralTofullCulture["ru"] = "ru-ru";
            _neutralTofullCulture["sa"] = "sa-in";
            _neutralTofullCulture["sr"] = "sr-latn-cs";
            _neutralTofullCulture["sr-cyrl"] = "sr-cyrl-cs";
            _neutralTofullCulture["sr-latn"] = "sr-latn-cs";
            _neutralTofullCulture["sk"] = "sk-sk";
            _neutralTofullCulture["sl"] = "sl-si";
            _neutralTofullCulture["es"] = "es-es";
            _neutralTofullCulture["sw"] = "sw-ke";
            _neutralTofullCulture["sv"] = "sv-se";
            _neutralTofullCulture["syr"] = "syr-sy";
            _neutralTofullCulture["ta"] = "ta-in";
            _neutralTofullCulture["tt"] = "tt-ru";
            _neutralTofullCulture["te"] = "te-in";
            _neutralTofullCulture["th"] = "th-th";
            _neutralTofullCulture["tr"] = "tr-tr";
            _neutralTofullCulture["uk"] = "uk-ua";
            _neutralTofullCulture["ur"] = "ur-pk";
            _neutralTofullCulture["uz-latn-uz"] = "uz-latn-uz";
            _neutralTofullCulture["uz-cyrl"] = "uz-cyrl-uz";
            _neutralTofullCulture["uz-latn"] = "uz-latn-uz";
            _neutralTofullCulture["vi"] = "vi-vn";
        }

        //--- Extension Methods

        /// <summary>
        /// Get a non-neutral culture for a given culture
        /// </summary>
        /// <param name="culture">Source culture</param>
        /// <param name="default">Default culture to return should there no non-neutral culture exist for the input culture.</param>
        /// <returns>A non-neutral <see cref="CultureInfo"/> instance.</returns>
        public static CultureInfo GetNonNeutralCulture(this CultureInfo culture, CultureInfo @default) {
            if(culture == null) {
                return @default;
            }
            if(!culture.IsNeutralCulture) {
                return culture;
            }
            return GetNonNeutralCulture(culture.Name) ?? @default;
        }

        //--- Class Methods ---

        /// <summary>
        /// Returns a non-neutral culture for a given language. Will return the specified culture, if it is already non-neutral
        /// or look up a matching non-neutral culture. Will only return null if no match can be found.
        /// </summary>
        /// <param name="language"></param>
        /// <returns>Non-neutral culture or null if no culture exists for specified language</returns>
        public static CultureInfo GetNonNeutralCulture(string language) {
            if(string.IsNullOrEmpty(language)) {
                return null;
            }
            try {
                CultureInfo culture = CultureInfo.GetCultureInfo(language);
                string nonNeutralLanguage;
                if(culture.IsNeutralCulture && _neutralTofullCulture.TryGetValue(language, out nonNeutralLanguage)) {
                    culture = CultureInfo.GetCultureInfo(nonNeutralLanguage);
                }
                return culture;
            } catch {
                return null;
            }
        }
    }
}
