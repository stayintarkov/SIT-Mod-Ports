using System;
using System.Collections.Generic;

namespace Replacements;

public class Replacements2
{
    private bool bool_0 = true;
    
    private readonly HashSet<AbstractAiCoreAgentM> hashSet_0 = new HashSet<AbstractAiCoreAgentM>();
    
    private readonly HashSet<AbstractAiCoreAgentM> hashSet_1 = new HashSet<AbstractAiCoreAgentM>();
    
    private readonly HashSet<AbstractAiCoreAgentM> hashSet_2 = new HashSet<AbstractAiCoreAgentM>();
    
    public void Update()
    {
        DoUpdate(bool_0, hashSet_0, hashSet_1, hashSet_2);
    }

    public static void DoUpdate(bool bool_0, HashSet<AbstractAiCoreAgentM> hashSet_0, HashSet<AbstractAiCoreAgentM> hashSet_1, HashSet<AbstractAiCoreAgentM> hashSet_2)
    {
        if (!bool_0)
            return;
        if (hashSet_1.Count > 0)
        {
            foreach (AbstractAiCoreAgentM abstractAiCoreAgentM in hashSet_1)
                hashSet_0.Remove(abstractAiCoreAgentM);
        }
        foreach (AbstractAiCoreAgentM abstractAiCoreAgentM in hashSet_0)
        {
            try
            {
                abstractAiCoreAgentM.Update();
            }
            catch (Exception ex)
            {
                if (!hashSet_2.Contains(abstractAiCoreAgentM))
                    hashSet_2.Add(abstractAiCoreAgentM);
            }
        }
    }
}