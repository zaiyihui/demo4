using System;

namespace ComputerCompanion.Services;

public interface ISecurityService
{
    string SignMessage(string message);
    bool VerifySignature(string message, string signature);
    string GenerateSessionKey();
    bool ValidateSessionKey(string sessionKey);
}