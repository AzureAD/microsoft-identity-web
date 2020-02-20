---
name: Bug report
about: Please do NOT file bugs without filling in this form.
title: '[Bug] '
labels: ''
assignees: ''

---

**Which Version of Microsoft Identity Web are you using ?**
Note that to get help, you need to run the latest version. 
<!-- E.g. Microsoft Identity Web 1.0.0-preview -->

**Where is the issue?**
* Web App
    * [ ] Sign-in users
    * [ ] Sign-in users and call web APIs
* Web API
    * [ ] Protected web APIs (Validating tokens)
    * [ ] Protected web APIs (Validating scopes)
    * [ ] Protected web APIs call downstream web APIs
* Token cache serialization
     * [ ] In Memory caches
     * [ ] Session caches
     * [ ] Distributed caches

Other? - please describe;

**Is this a new or existing app?**
<!-- Ex:
a. The app is in production, and I have upgraded to a new version of Microsoft Identity Web
b. The app is in production, I haven't upgraded Microsoft Identity Web, but started seeing this issue
c. This is a new app or experiment
-->

**Repro**

```csharp
var your = (code) => here;
```

**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. exception is thrown, UI freezes  

**Possible Solution**
<!--- Only if you have suggestions on a fix for the bug -->

**Additional context/ Logs / Screenshots**
Add any other context about the problem here, such as logs and screenshots.