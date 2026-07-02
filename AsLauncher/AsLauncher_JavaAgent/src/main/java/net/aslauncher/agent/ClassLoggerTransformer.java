package net.aslauncher.agent;

import java.lang.instrument.ClassFileTransformer;
import java.lang.instrument.IllegalClassFormatException;
import java.security.ProtectionDomain;

public class ClassLoggerTransformer implements ClassFileTransformer
{
    private static final String TARGET_CLASS = "com/mojang/authlib/yggdrasil/YggdrasilSocialInteractionsService";

    @Override
    public byte[] transform(
            ClassLoader loader,
            String className,
            Class<?> classBeingRedefined,
            ProtectionDomain protectionDomain,
            byte[] classfileBuffer)
            throws IllegalClassFormatException
    {
        if (className == null)
        {
            return null;
        }

        try
        {
            if (TARGET_CLASS.equals(className))
            {
                System.out.println("[AsL-Agent] TARGET FOUND");

                return YggdrasilPatcher.patch(classfileBuffer);
            }
        }
        catch (Throwable t)
        {
            System.out.println("[AsL-Agent] ERROR: " + t);
            return null;
        }

        return null;
    }
}