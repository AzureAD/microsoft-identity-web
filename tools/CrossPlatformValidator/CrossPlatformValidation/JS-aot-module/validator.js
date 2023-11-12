// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

const Validator = require('./bin/aot-module').Validator;

// Call a method exported by the .NET module.
Validator.initialize("https://login.microsoftonline.com/organizations", "f4aa5217-e87c-42b2-82af-5624dd14ee72");
result = Validator.validate("Bearer EncodedHeader.EndcodedPayload.EncodedSignature");
process.stdout.write(result)
