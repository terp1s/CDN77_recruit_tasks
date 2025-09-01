class DNS
{
    public (int, int)? PopLookup(string ecs, IPTrie routing_data)
    {
        BitArray ip = ParseIPv6ToBitArray(ecs);
        int firstChar = BitsToInt(ip, 0);
        IPNode node = routing_data.Root;
        IPEdge edge = node.Edges[firstChar];
        int ipLength = 0;
        (int, int)? bestPop = null;

        while (edge != null)
        {
            firstChar = BitsToInt(ip, ipLength);

            if (EdgeInIP(edge, ip, ipLength))
            {
                ipLength += edge.Length;
                node = edge.End;
                edge = node.Edges[firstChar];
                if(node.Pop != null)
                {
                    bestPop = node.Pop;
                }
            }
            else
            {
                break;
            }
        }

        return bestPop;
    }

    private static bool EdgeInIP(IPEdge edge, BitArray ip, int ipIndex)
    {
        if (ip.Length - ipIndex < edge.Length) //source prefix length < scope prefix length
        {
            return false;
        }

        for (int i = 0; i < edge.Length; i++)
        {
            if (ip[ipIndex + i] != edge.Data[i])
            {
                return false;
            }
        }

        return true;
    }
    private static BitArray ParseIPv6ToBitArray(string ipv6)
    {
        var ip = IPAddress.Parse(ipv6);
        byte[] bytes = ip.GetAddressBytes();
        return new BitArray(bytes);
    }
    static int BitsToInt(BitArray bits, int start)
    {
        int value = 0;
        for (int i = 0; i < 4; i++)
        {
            value <<= 1;
            if (bits[start + i]) value |= 1;
        }
        return value;
    }
}
