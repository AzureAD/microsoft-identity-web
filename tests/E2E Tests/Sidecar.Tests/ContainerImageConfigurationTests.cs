// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Xunit;

namespace Sidecar.Tests;

public class ContainerImageConfigurationTests
{
    [Fact]
    public void NanoServerDockerfile_UsesBuiltInContainerUser()
    {
        // Arrange
        string dockerfilePath = Path.Combine(AppContext.BaseDirectory, "DockerFile.NanoServer");

        // Act
        string dockerfile = File.ReadAllText(dockerfilePath);

        // Assert
        Assert.Contains("USER ContainerUser", dockerfile, StringComparison.Ordinal);
        Assert.DoesNotContain("USER app", dockerfile, StringComparison.Ordinal);
    }
}
