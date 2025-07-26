// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AgentApplicationsTests
{
     [CollectionDefinition(nameof(TokenAcquirerFactorySingletonProtection))]
     public class TokenAcquirerFactorySingletonProtection
     {
         // This class has no code, and is never created. Its purpose is to prevent test classes using the
         // static singleton DefaultTokenAcquirerFactory from running in parallel as some tests modify this singleton.
    }
}
