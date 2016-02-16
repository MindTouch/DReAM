MindTouch DReAM
===============

Welcome to MindTouch DReAM.

Notes on Contributing to DReAM
------------------------------
If you have a new feature for DReAM, please work against master, however, 
if you have a patch you'd like to see in the next point release, please 
base your work on the release branch (currently 2.2).

If you submit a pull request for master, your changes will not make it
into an official releas until the next release branch is created
(current target, 2.3)

1. What is MindTouch DReAM?
---------------------------
MindTouch DReAM is a REST-based distributed application framework
developed in Mono/.NET. With DReAM, a Web service is similar to an
object, and features interact through standard HTTP verbs. This design
allows the developer to assume an "idealized" world where everything a
service comes into contact with is accessed through Web requests. The
DReAM service library addresses common problems, and the DReAM runtime
orchestrates all interactions without requiring a Web server to be
pre-installed on a target machine.

MindTouch DReAM manages all the complex aspects of interactive web
services, such as providing storage locations, database connections,
event notifications, automatic data conversion from XML to JSON and
short-circuit communication for co-hosted services. The platform enables
developers to create enterprise-ready service architectures with
exceptional speed and ease.

With DReAM, developers can create innovative services without worrying
about the underlying infrastructure. Moreover, developers can use his or
her programming language of choice and still leverage an open-source
platform for creating rich, interactive Web services that can generate
and combine data from multiple sources. With MindTouch DReAM, developers
can create interactive REST-based Web services for any platform,
including Linux, Mac OS and the entire Microsoft platform, as well as
virtually any device, from server to desktop to mobile device. MindTouch
DReAM is not new technology. What's new is the ease with which
developers can now develop and distribute compelling Web services,
because "behavior" is now mobile, not just data. MindTouch DReAM is
built on the premise that everything is remote.


2. System Requirements
----------------------
* To run
	* Microsoft Visual Studio 2008 with .Net 3.5 or later
	* Mono v2.4 or later
* To build
	* Microsoft Visual Studio 2008 with .Net 3.5 or later
	* Mono v2.8 or later

3. License
----------
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
