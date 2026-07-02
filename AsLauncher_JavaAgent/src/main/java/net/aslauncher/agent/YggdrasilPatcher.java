package net.aslauncher.agent;

import org.objectweb.asm.ClassReader;
import org.objectweb.asm.ClassWriter;
import org.objectweb.asm.Opcodes;
import org.objectweb.asm.tree.*;

public class YggdrasilPatcher
{
    public static byte[] patch(byte[] bytes)
    {
        System.out.println("[AsL-Agent] PATCHING...");

        ClassNode classNode = new ClassNode();

        ClassReader reader = new ClassReader(bytes);

        reader.accept(classNode, 0);

        for (MethodNode method : classNode.methods)
        {
            if ("()Z".equals(method.desc) && ("serversAllowed".equals(method.name)
                                           || "realmsAllowed".equals(method.name)
                                           || "chatAllowed".equals(method.name)))
            {
                System.out.println("[AsL-Agent] PATCHING METHOD: " + method.name);

                method.instructions.clear();
                method.tryCatchBlocks.clear();

                method.instructions.add(new InsnNode(Opcodes.ICONST_1));
                method.instructions.add(new InsnNode(Opcodes.IRETURN));

                method.maxStack = 1;
                method.maxLocals = 1;
            }
        }

        ClassWriter writer = new ClassWriter(ClassWriter.COMPUTE_FRAMES | ClassWriter.COMPUTE_MAXS);

        classNode.accept(writer);

        System.out.println("[AsL-Agent] PATCH SUCCESSFULLY");

        return writer.toByteArray();
    }
}