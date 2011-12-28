using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;

namespace MindTouch.Xml
{
    public static class XPathUtils
    {
        public static string GetDefaultPrefix(XmlNamespaceManager namespaces)
        {
            if (String.IsNullOrEmpty(namespaces.LookupNamespace(string.Empty)))
                return null;
            
            //find a non-used prefix
            string pref = null;
            int cont = 0;            
            do
            {
                cont++;
                pref = "def" + cont;
                
            } while (!String.IsNullOrEmpty(namespaces.LookupNamespace(pref)));
            namespaces.AddNamespace(pref, namespaces.LookupNamespace(string.Empty));
            return pref;
        }
				
		// this code is obtained from
		// http://stackoverflow.com/a/2054877/19046
        public static string GetPrefixedPath(string xPath, string prefix)
        {
            char[] validLeadCharacters = "@/".ToCharArray();
            char[] quoteChars = "\'\"".ToCharArray();

            List<string> pathParts = xPath.Split("/".ToCharArray()).ToList();

            string result = string.Join("/",
                                    pathParts.Select(
                                        x =>
                                        (string.IsNullOrEmpty(x) ||
                                         x.IndexOfAny(validLeadCharacters) == 0 ||
                                         (x.IndexOf(':') > 0 &&
                                          (x.IndexOfAny(quoteChars) < 0 || x.IndexOfAny(quoteChars) > x.IndexOf(':'))))
                                            ? x
                                            : prefix + ":" + x).ToArray());
            return result;
        }

        public static void RemoveDefaultPrefix(string prefix, XmlNamespaceManager namespaces)
        {
            if (prefix == null)
                return;
            string uri = namespaces.LookupNamespace(prefix);
            namespaces.RemoveNamespace(prefix, uri);
        }
        
    }
}
