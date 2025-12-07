Dependabot helps manage your dependencies in three different ways:

* Dependabot alerts—inform you about vulnerabilities in the dependencies that you use in your repository.
* Dependabot security updates—automatically raise pull requests to update the dependencies you use that have known security vulnerabilities.
* Dependabot version updates—automatically raise pull requests to keep your dependencies up-to-date.

Dependabot Version Updates are what we will focus on to remain up to date on Identity Web Releases.

1. On GitHub.com, navigate to the main page of the repository.
2. Under your repository name, click **Settings**. If you cannot see the "Settings" tab, select the ••• dropdown menu, then click Settings.
![image](https://github.com/AzureAD/microsoft-identity-web/assets/69649063/7bc7f5ed-9447-4e41-af43-985d2bd3f049)
3. In the "Security" section of the sidebar, click **Code security and analysis**.
4. Under "Code security and analysis", to the right of Dependabot alerts, click **Enable** for Dependabot version updates.
5. On the next page, an editor will be open to add dependabot.yml to the .github folder of your repo. Add the following to the YAML file:

```text
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "daily"
    allow:
      - dependency-name: "Microsoft.Identity*"
    labels:
      - "dependabot"
      - "dependencies"
```
6. Click **Commit Changes** to add dependabot to your repo. Now Dependabot can automatically raise pull requests to keep Identity Web up-to-date.