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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace MindTouch.Tools {

    // Control used to enter a query or URL parameter value
    public partial class ParameterControl : UserControl {

        //--- Constructors ---

        public ParameterControl(String paramName, String defaultParamValue, bool optional, bool hasValue) {
            InitializeComponent();
            paramLabel.Text = paramName;
            paramTextBox.Text = defaultParamValue;
            includeCheckBox.Visible = optional;
            includeCheckBox.Checked = (null != defaultParamValue) && (!defaultParamValue.EndsWith("?"));
            if (!hasValue) {
                paramTextBox.Visible = false;
            }
        }

        //--- Events ---
        
        public event EventHandler DataChanged;

        //--- Event Handlers ---

        private void paramTextBox_TextChanged(object sender, EventArgs e) {
            includeCheckBox.Checked = true;
            if (null != DataChanged) {
                DataChanged(sender, e);
            }
        }

        private void includeCheckBox_CheckedChanged(object sender, EventArgs e) {
            if (null != DataChanged) {
                DataChanged(sender, e);
            }
        }

        //--- Properties ---

        public String ParameterName {
            get{
                return paramLabel.Text;
            }
        }

        public String ParameterValue {
            get {
                if (paramTextBox.Visible) {
                    return paramTextBox.Text;
                }
                else {
                    return null;
                }
            }
        }

        public Boolean IncludeInUrl {
            get {
                return includeCheckBox.Visible ? includeCheckBox.Checked : true ;
            }
        }
    }
}
