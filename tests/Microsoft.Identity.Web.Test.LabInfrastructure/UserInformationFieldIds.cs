// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Web.Test.Common;

namespace Microsoft.Identity.Web.Test.LabInfrastructure
{
    public class UserInformationFieldIds
    {
        private string _passwordInputId;
        private string _passwordSignInButtonId;

        public string GetPasswordInputId()
        {
            if (string.IsNullOrWhiteSpace(_passwordInputId))
            {
                DetermineFieldIds();
            }

            return _passwordInputId;
        }

        public string GetPasswordSignInButtonId()
        {
            if (string.IsNullOrWhiteSpace(_passwordSignInButtonId))
            {
                DetermineFieldIds();
            }

            return _passwordSignInButtonId;
        }

        /// <summary>
        /// When starting auth, the first screen, which collects the username, is from AAD.
        /// </summary>
        public string AADSignInButtonId
        {
            get
            {
                return TestConstants.WebSubmitId;
            }
        }

        /// <summary>
        /// When starting auth, the first screen, which collects the username, is from AAD.
        /// </summary>
        public string AADUsernameInputId
        {
            get
            {
                return TestConstants.WebUPNInputId;
            }
        }

        private void DetermineFieldIds()
        {
            _passwordInputId = TestConstants.WebPasswordId;
            _passwordSignInButtonId = TestConstants.WebSubmitId;
        }
    }
}
