package net.aslauncher.agent;

import java.lang.instrument.Instrumentation;

public class Agent
{
    public static void premain(String args, Instrumentation inst)
    {
        System.out.println("[AsL-Agent] STARTED");

        inst.addTransformer(new ClassLoggerTransformer());
    }
}