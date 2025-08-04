// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Identity.Web.Test.Common;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Identity.Web.Test
{
    [Collection(nameof(TokenAcquirerFactorySingletonProtection))]
    public class TokenAcquirerFactoryTests
    {
        private readonly int numberOfIterations = 5;

        [Fact]
        public void Build_ThrowsInvalidOperationException_WhenCalledTwiceConcurrently()
        {
            // Locally this test failed appropriately on the first run every time when the relevant locks were removed.
            // However, for robustness, we run it multiple times to protect against false positives in the future.
            for (int i = 0; i < numberOfIterations; i++)
            {
                // Arrange
                TokenAcquirerFactory.ResetDefaultInstance();
                var testFactory = TokenAcquirerFactory.GetDefaultInstance();

                // Act & Assert
                try
                {
                    var exception = Assert.Throws<AggregateException>(() =>
                    {
                        Parallel.Invoke(
                            () => testFactory.Build(),
                            () => testFactory.Build()
                        );
                    });
                    Assert.Single(exception.InnerExceptions);
                    Assert.All(exception.InnerExceptions, ex => Assert.IsType<InvalidOperationException>(ex));
                }
                catch (Exception ex)
                {
                    throw new Exception($"Test failed on iteration {i}", ex);
                }
            }
        }

        [Fact]
        public void GetDefaultInstance_ParallelExecutionGeneric()
        {
            // Locally this test failed appropriately on the first run every time when the relevant locks were removed.
            // However, for robustness, we run it multiple times to protect against false positives in the future.
            for (int i = 0; i < numberOfIterations; i++)
            {
                // Arrange
                TokenAcquirerFactoryWithCounter.ResetDefaultInstance();
                // Act
                Parallel.Invoke(
                    () => TokenAcquirerFactoryWithCounter.GetDefaultInstance<TokenAcquirerFactoryWithCounter>(),
                    () => TokenAcquirerFactoryWithCounter.GetDefaultInstance<TokenAcquirerFactoryWithCounter>(),
                    () => TokenAcquirerFactoryWithCounter.GetDefaultInstance<TokenAcquirerFactoryWithCounter>(),
                    () => TokenAcquirerFactoryWithCounter.GetDefaultInstance<TokenAcquirerFactoryWithCounter>()
                );
                // Assert
                Assert.Equal(1, TokenAcquirerFactoryWithCounter.InstanceCount);
            }
        }

        /// <summary>
        /// Due to how the non-Generic GetDefaultInstance method creates a new TokenAcquirerFactory instance immediately, it is not easy to 
        /// test concurrent access without adding a counter to the TokenAcquirerFactory class. This test is for manual testing only to avoid 
        /// adding a counter to the class and method permanently so to use this test uncomment the lines using s_defaultInstanceCounter, make
        /// the member inside the TokenAcquirerFactory class, and increment in the constructor. Don't forget to reset it between tests.
        /// The current lock implementation as of March 2025 has been tested to work correctly.
        /// </summary>
        [Fact(Skip = "Requires manual testing")]
        public void GetDefaultInstance_ParallelExecutionNonGeneric()
        {
            // Arrange
            TokenAcquirerFactory.ResetDefaultInstance();
            //TokenAcquirerFactory.s_defaultInstanceCounter = 0;

            // Act
            Parallel.Invoke(
                () => TokenAcquirerFactory.GetDefaultInstance(),
                () => TokenAcquirerFactory.GetDefaultInstance(),
                () => TokenAcquirerFactory.GetDefaultInstance(),
                () => TokenAcquirerFactory.GetDefaultInstance()
            );

            // Assert
            //Assert.Equal(1, TokenAcquirerFactory.s_defaultInstanceCounter);
        }

        [Fact]
        public void DefineConfiguration_HandlesNullFromPathGetDirectoryName()
        {
            // Arrange
            var factory = new TestTokenAcquirerFactory();
            
            // Act & Assert
            // This should not throw an exception even if Path.GetDirectoryName returns null
            string result = factory.TestDefineConfiguration();
            
            // Verify result is not null or empty
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    public class TestTokenAcquirerFactory : TokenAcquirerFactory
    {
        public string TestDefineConfiguration()
        {
            return DefineConfiguration(new ConfigurationBuilder());
        }
    }

    public class TokenAcquirerFactoryWithCounter : TokenAcquirerFactory
    {
        public static int InstanceCount { get; private set; } = 0;

        public TokenAcquirerFactoryWithCounter()
        {
            InstanceCount++;
        }

        public static new void ResetDefaultInstance()
        {
            TokenAcquirerFactory.ResetDefaultInstance();
            InstanceCount = 0;
        }
    }
}
