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
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Microsoft.Win32;

using MindTouch.IO;
using MindTouch.Dream;
using MindTouch.Web;
using MindTouch.Xml;

namespace MindTouch.Tools {

    // Facilitates invoking Dream API's specified by a service blueprint
    public partial class DreamAPITest : Form {

        // --- Fields ---

        String _url = null;                         // the base url  
        XDoc _blueprintDoc = null;                  // the service blueprint of the url
        NameValueCollection _defaultValues = null;  // contains a collection of default values
        Control[] _parameterControls = null;        // the collection of url parameter input controls
        Control[] _queryControls = null;            // the collection of url query input controls
        DreamCookieJar _cookies = new DreamCookieJar();


        // --- Constructors ---

        public DreamAPITest() {
            InitializeComponent();
            LoadDefaults();
        }

        // --- Event Handlers ---

        private void loadBlueprintFromToolStripMenuItem_Click(object sender, EventArgs e) {

            // query the user for the blueprint address
            LoadBlueprintDialog dialog = new LoadBlueprintDialog();
            dialog.ShowDialog();

            // load and display the blueprint 
            try {
                if (DialogResult.OK == dialog.DialogResult) {
                    Plug p = Plug.New(dialog.Url + "/@blueprint");
                    DreamMessage msg = p.Get();
                    SetCurrentBluebrint(dialog.Url, msg.ToDocument());
                }
            } catch (Exception) {
                SetCurrentBluebrint(null, null);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void apiComboBox_SelectedIndexChanged(object sender, EventArgs e) {

            // update the parameters and url when the api selection is changed
            GenerateParamsForFeature(GetSelectedFeature());
            urlTextBox.Text = GenerateUrlFromParams();
        }

        private void apiComboBox_TextUpdate(object sender, EventArgs e) {
            // update the parameters and url when the api selection is changed
            GenerateParamsForFeature(GetSelectedFeature());
            urlTextBox.Text = GenerateUrlFromParams();
        }

        private void openFileButton_Click(object sender, EventArgs e) {

            // query the user for a file 
            OpenFileDialog dialog = new OpenFileDialog();
            if (DialogResult.OK == dialog.ShowDialog(this)) {
                filepathTextBox.Text = dialog.FileName;
            }

        }

        private void submitButton_Click(object sender, EventArgs e) {

            // submit the request
            ExecuteRequest(GetOperationType(), urlTextBox.Text);

        }

        private void updateUrlButton_Click(object sender, EventArgs e) {

            // re-geerate the url based on the current parameter values
            String url = GenerateUrlFromParams();
            urlTextBox.Text = url;

        }

        private void webBrowser_Navigating(object sender, WebBrowserNavigatingEventArgs e) {

            // when we navigate away from a page, delete the temporary file corresponding to it
            try {
                if (null != webBrowser.Url) {
                    File.Delete(webBrowser.Url.LocalPath);
                }
            } catch {
            }
        }

        private void APIManualTest_FormClosed(object sender, FormClosedEventArgs e) {
            // when we close the app, delete the temporary file if there is one
            try {
                if (null != webBrowser.Url) {
                    File.Delete(webBrowser.Url.LocalPath);
                }
            } catch {
            }
        }

        private void filepathTextBox_TextChanged(object sender, EventArgs e) {
            useFileRadioButton.Checked = true;
        }

        private void messageContentTextBox_TextChanged(object sender, EventArgs e) {
            useTextRadioButton.Checked = true;
        }

        void parameterControl_DataChanged(object sender, EventArgs e) {
            String url = GenerateUrlFromParams();
            urlTextBox.Text = url;
        }

        // --- Methods ---

        private void LoadDefaults() {

            // load default parameter values from the xml data file
            _defaultValues = new NameValueCollection();
            try {
                XDoc defaultDoc = XDocFactory.LoadFrom("DefaultData.xml", MimeType.XML);
                foreach (XDoc defaultValue in defaultDoc["/params/param"]) {
                    _defaultValues.Add(defaultValue["name"].Contents.Trim(), defaultValue["default"].Contents.Trim());
                }
            } catch { }
        }

        private XDoc GetSelectedFeature() {

            // Retrieve the feature information for the current selection
            XDoc selectedFeature = _blueprintDoc[string.Format("/blueprint/features/feature[pattern='{0}']", apiComboBox.Text)];
            return !selectedFeature.IsEmpty ? selectedFeature : null;
        }

        private void SetCurrentBluebrint(String url, XDoc blueprintDoc) {
            _url = url;
            _blueprintDoc = blueprintDoc;
            bool hasBlueprint = (!String.IsNullOrEmpty(url)) && (null != blueprintDoc);

            // clear the list of blueprint api's
            apiComboBox.Items.Clear();
            if (hasBlueprint) {

                // add each api associated with the blueprint
                ArrayList features = new ArrayList();
                foreach (XDoc feature in _blueprintDoc["/blueprint/features/feature"]) {
                    features.Add(feature["pattern"].Contents);
                }
                String[] featuresList = (String[])features.ToArray(typeof(String));
                Array.Sort(featuresList, StringComparer.Ordinal);
                apiComboBox.Items.AddRange(featuresList);
                if (0 < apiComboBox.Items.Count) {
                    apiComboBox.SelectedIndex = 0;
                }

                toolStripStatusLabel.Text = "Blueprint loaded:  " + _url;
            } else {
                toolStripStatusLabel.Text = "Error occurred while loading blueprint";
            }

            // update the interface based on whether we have a blueprint
            loginLabel.Visible = hasBlueprint;
            connectionPanel.Visible = hasBlueprint;
            connectionPanel.Visible = hasBlueprint;
            parametersLabel.Visible = hasBlueprint;
            parametersPanel.Visible = hasBlueprint;
            messageContentLabel.Visible = hasBlueprint;
            messageContentPanel.Visible = hasBlueprint;
            submitPanel.Visible = hasBlueprint;
        }

        private void GenerateParamsForFeature(XDoc feature) {

            // clear the parameters list
            parametersPanel.Controls.Clear();

            if (null != feature) {

                // add an item for each query parameter
                List<Control> controlsList = new List<Control>();
                foreach (XDoc queryParam in feature["param"]) {
                    string queryParamName = queryParam["name"].Contents;
                    if (!queryParamName.StartsWith("{")) {
                        string queryParamValue = queryParam["valuetype"].Contents;
                        bool hasValue = !queryParam["valuetype"].IsEmpty;
                        ParameterControl parameterControl = new ParameterControl(queryParamName, queryParamValue, true, hasValue);
                        parameterControl.Dock = DockStyle.Top;
                        parameterControl.DataChanged += new EventHandler(parameterControl_DataChanged);
                        controlsList.Add(parameterControl);
                    }
                }
                _queryControls = controlsList.ToArray();
                parametersPanel.Controls.AddRange(_queryControls);

                // add an item for each url parameter
                String pattern = feature["pattern"].Contents;
                Regex regex = new Regex((@"{(?<param>[^}]+)}"), RegexOptions.CultureInvariant);
                MatchCollection matches = regex.Matches(pattern);
                _parameterControls = new Control[matches.Count];
                String[] parameterNames = new String[matches.Count];
                for(int i = 0; i < matches.Count; i++)
                {
                    string paramName = matches[i].Groups["param"].Value;
                    ParameterControl parameterControl = new ParameterControl(paramName, null==_defaultValues?String.Empty:_defaultValues[paramName], false, true);
                    parameterControl.Dock = DockStyle.Top;
                    parameterControl.DataChanged += new EventHandler(parameterControl_DataChanged);
                    _parameterControls[_parameterControls.Length - 1 - i] = parameterControl;
                    parameterNames[_parameterControls.Length - 1 - i] = paramName;
                }
                parametersPanel.Controls.AddRange(_parameterControls);

                parametersPanel.Refresh();

                // in the case of a write operation, enable the panel for attaching content
                if ("DELETE" == GetOperationType() ||
                    "POST" == GetOperationType() ||
                    "PUT" == GetOperationType()) {
                    messageContentPanel.Enabled = true;
                    messageContentTextBox.Text = _defaultValues[pattern];
                } else {
                    messageContentPanel.Enabled = false;
                }
            }

        }

        private String GenerateUrlFromParams() {

            String result = _url;
            result += "/";
            XDoc selectedFeature = GetSelectedFeature();
            if (null != selectedFeature) {
                String pattern = selectedFeature["pattern"].Contents;
                String[] urlString = pattern.Split(':');
                result += urlString[1];

                // replace each url parameter with the user specified value
                if (null != _parameterControls) {
                    foreach (Control control in _parameterControls) {
                        ParameterControl parameterControl = control as ParameterControl;
                        result = result.Replace("{" + parameterControl.ParameterName + "}", parameterControl.ParameterValue);
                    }
                }

                // include each selected query parameter value
                if (null != _queryControls) {
                    List<ParameterControl> includedParameters = new List<ParameterControl>();
                    foreach (Control control in _queryControls) {
                        ParameterControl parameterControl = control as ParameterControl;
                        if (parameterControl.IncludeInUrl) {
                            includedParameters.Add(parameterControl);
                        }
                    }
                    if (0 < includedParameters.Count) {
                        result += "?";
                        for (int i = includedParameters.Count - 1; i >= 0; i--) {
                            result += includedParameters[i].ParameterName;
                            if (null != includedParameters[i].ParameterValue) {
                                result += "=" + includedParameters[i].ParameterValue;
                            }
                            if (i != 0) {
                                result += "&";
                            }
                        }
                    }
                }
            }
            return result;
        }

        private String GetOperationType() {

            // parse the feature information to retrieve the operation type
            XDoc selectedFeature = GetSelectedFeature();
            if (null != selectedFeature) {
                String pattern = selectedFeature["pattern"].Contents;
                String[] urlString = pattern.Split(':');
                return urlString[0];
            }
            return null;
        }

        private DreamMessage GetDreamMessage(XUri uri) {

            // create a dream message from a file or from text entered by the user
            DreamMessage msg = null;
            if (useFileRadioButton.Checked) {
                msg = DreamMessage.FromFile(filepathTextBox.Text);
            } else {
                MimeType mimeType = MimeType.TEXT;
                MimeType.TryParse(mimeTypeComboBox.Text, out mimeType);
                msg = DreamMessage.Ok(mimeType, messageContentTextBox.Text);
            }
            msg.Cookies.AddRange(_cookies.Fetch(uri));
            return msg;

        }

        private void SetRequestResult(DreamMessage result, XUri uri) {
            String tempfilename = String.Empty;
            
            _cookies.Update(result.Cookies, uri);

            txtHeaders.Clear();
            foreach (KeyValuePair<string, string> header in result.Headers) {
                txtHeaders.Text += header.Key + " = " + header.Value + Environment.NewLine;
            }

            foreach (DreamCookie c in result.Cookies) {
                txtHeaders.Text += string.Format("Cookie: '{0}' Value: '{1}' Path: '{2}', Expires: '{3}' Domain: '{4}' {5}", c.Name, c.Value, c.Path, c.Expires, c.Domain, Environment.NewLine);
            }

            // if we received a file, save it
            if (!String.IsNullOrEmpty(result.Headers[DreamHeaders.CONTENT_DISPOSITION])) {
                String filename;
                String[] contentInfoList = result.Headers[DreamHeaders.CONTENT_DISPOSITION].Split(';');
                foreach (String contentInfo in contentInfoList) {
                    String trimmedContentInfo = contentInfo.Trim();
                    if (StringUtil.StartsWithInvariantIgnoreCase(trimmedContentInfo, "filename=")) {
                        filename = trimmedContentInfo.Substring(9);
                        filename = filename.Trim('"', '\\');
                        tempfilename = Path.Combine(Path.GetTempPath(), filename);
                        result.ToStream().CopyToFile(tempfilename, result.ContentLength);
                    }
                }
            } else {
                tempfilename = Path.Combine(Path.GetTempPath(), DateTime.Now.Ticks.ToString()) + ".xml";
                RegistryKey key = Registry.ClassesRoot.OpenSubKey("MIME\\Database\\Content Type\\" + result.ContentType.FullType);
                if (null != key) {
                    tempfilename = tempfilename + key.GetValue("Extension");
                    key.Close();
                }
                File.WriteAllBytes(tempfilename, result.ToBytes());
            }

            // browse to the saved temporary file
            if (!String.IsNullOrEmpty(tempfilename))
            {
                webBrowser.Url = new Uri(tempfilename);
            }
            else{
                webBrowser.Url = null;
            }

            tabControl1.SelectedTab = resultsTabPage;
        }

        private void ExecuteRequest(String operation, String url) {
            DreamMessage msg = null;
            XUri uri = null;
            try {
                uri = new XUri(url);

                // exectue the specified request and display the results
                switch (operation.ToUpper()) {
                    case "DELETE": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = p.DeleteAsync(GetDreamMessage(uri)).Wait();
                            SetRequestResult(msg, uri);
                            break;
                        }
                    case "GET": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = DreamMessage.Ok();
                            msg.Cookies.AddRange(_cookies.Fetch(uri));
                            msg = p.GetAsync(msg).Wait();
                            SetRequestResult(msg, uri);
                            break;
                        }
                    case "HEAD": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = p.InvokeAsync("HEAD", DreamMessage.Ok()).Wait();
                            if (DreamStatus.Ok == msg.Status) {
                                StringBuilder result = new StringBuilder();
                                foreach(KeyValuePair<string, string> header in msg.Headers) {
                                    result.Append(header.Key);
                                    result.Append("=");
                                    result.Append(header.Value);
                                    result.Append(Environment.NewLine);
                                }
                                msg = DreamMessage.Ok(MimeType.TEXT, result.ToString());
                            }
                            SetRequestResult(msg, uri);
                            break;
                        }
                    case "POST": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = p.PostAsync(GetDreamMessage(uri)).Wait();
                            SetRequestResult(msg, uri);             
                            break;
                        }
                    case "PUT": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = p.PutAsync(GetDreamMessage(uri)).Wait();
                            SetRequestResult(msg, uri);
                            break;
                        }
                    case "OPTIONS": {
                            Plug p = Plug.New(url).WithCredentials(loginTextBox.Text, passwordTextBox.Text);
                            msg = p.OptionsAsync().Wait();
                            SetRequestResult(msg, uri);
                            break;
                        }
                }
            } catch (DreamResponseException error) {
                SetRequestResult(error.Response, uri);
            } catch (Exception error) {
                SetRequestResult(DreamMessage.Ok(MimeType.TEXT, error.ToString()), uri);
            }
        }
    }
}