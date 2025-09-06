class IPTrie
{
    public IPNode Root;
}
class IPEdge
{
    public int Length => Data.Length;
    public bool[] Data;
    public IPNode End;
}
class IPNode
{
    public (int, int)? Pop;
    public IPEdge ZeroChild { get; set; }
    public IPEdge OneChild { get; set; }

    public IPEdge GetEdge(bool b)
    {
        if (b)
        {
            return OneChild;
        }

        return ZeroChild;
    }
}
class Ecs
{
    public int ScopePrefixLength;
    public bool[] IPAddress { get; set; }
}
class DNS
{
    public static (int, int)? PopLookup(Ecs ecs, IPTrie routing_data)
    {
        bool[] ip = ecs.IPAddress;
        IPNode node = routing_data.Root;
        IPEdge edge = node.GetEdge(ip[0]);
        int ipLength = 0;
        int residue = ecs.ScopePrefixLength - ipLength;
        (int, int)? bestPop = node.Pop;

        while (edge != null && residue > edge.Length)
        {
            if (EdgeInIP(edge, ip, ipLength))
            {
                node = edge.End;
                ipLength += edge.Length;
                residue = ecs.ScopePrefixLength - ipLength;

                if (node.Pop != null)
                {
                    bestPop = node.Pop;
                }

                edge = node.GetEdge(ip[ipLength]);
            }
            else
            {
                break;
            }
        }

        if (edge != null && residue == edge.Length)
        {
            node = edge.End;
            if (node.Pop != null)
            {
                bestPop = node.Pop;
            }
        }

        return bestPop;
    }

    public static bool EdgeInIP(IPEdge edge, bool[] ip, int ipIndex)
    {
        for (int i = 0; i < edge.Length; i++)
        {
            if (ip[ipIndex + i] != edge.Data[i])
            {
                return false;
            }
        }

        return true;
    }
}