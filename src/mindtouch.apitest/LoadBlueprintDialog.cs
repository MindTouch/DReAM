/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2009 MindTouch, Inc.
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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MindTouch.Tools {

    // Gathers a service blueprint URL from the user
    public partial class LoadBlueprintDialog : Form {

        //--- Fields ---

        String _url = null;

        //--- Constructors ---

        public LoadBlueprintDialog() {
            InitializeComponent();
        }

        //--- Properites ---

        public String Url {
            get {
                return _url;
            }
        }

        //--- Event Handlers ---

        private void loadButton_Click(object sender, EventArgs e) {
            _url = urlTextBox.Text;
            DialogResult = DialogResult.OK;
        }
    }
}