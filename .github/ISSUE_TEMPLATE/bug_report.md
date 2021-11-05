---
name: Bug report
about: Please do NOT file bugs without filling in this form.
title: '[Bug] '
labels: ''
assignees: ''

---

**Which version of Microsoft Identity Web are you using?**
Note that to get help, you need to run the latest version. 
<!-- E.g. Microsoft Identity Web 1.0.0-preview -->

**Where is the issue?**
* Web app
    * [ ] Sign-in users
    * [ ] Sign-in users and call web APIs
* Web API
    * [ ] Protected web APIs (validating tokens)
    * [ ] Protected web APIs (validating scopes)
    * [ ] Protected web APIs call downstream web APIs
* Token cache serialization
     * [ ] In-memory caches
     * [ ] Session caches
     * [ ] Distributed caches
* Other (please describe)

**Is this a new or an existing app?**
<!-- Ex:
a. The app is in production and I have upgraded to a new version of Microsoft Identity Web.
b. The app is in production and I haven't upgraded Microsoft Identity Web, but started seeing this issue.
c. This is a new app or an experiment.
-->

**Repro**

```csharp
var your = (code) => here;
```

**Expected behavior**
A clear and concise description of what you expected to happen (or code).

**Actual behavior**
A clear and concise description of what happens, e.g. an exception is thrown, UI freezes.

**Possible solution**
<!--- Only if you have suggestions on a fix for the bug. -->

**Additional context / logs / screenshots / link to code**
<!-- Please do not include any customer data or Personal Identifiable Information (PII) in any content posted to GitHub. See https://docs.microsoft.com/en-us/compliance/regulatory/gdpr#gdpr-faqs for more info on PII.-->
Add any other context about the problem here, such as logs and screenshots, or even links to code.
