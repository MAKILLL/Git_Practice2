using System;
using UnityEngine;

public sealed class RouteField
{
    private const int Width = 72;
    private const int CellCount = Width * Width;

    private readonly int[] distances = new int[CellCount];
    private readonly int[] visitedAt = new int[CellCount];
    private readonly int[] heapNodes = new int[CellCount * 2];
    private readonly int[] heapCosts = new int[CellCount * 2];
    private int searchId;
    private int heapCount;

    public int RebuildRoutes(int routeCount, int seed)
    {
        var state = (uint)seed;
        var checksum = 0;
        for (var route = 0; route < routeCount; route++)
        {
            var start = NextOpenCell(ref state);
            var target = NextOpenCell(ref state);
            checksum = unchecked(checksum * 31 + FindRouteCost(start, target));
        }

        return checksum;
    }

    private int FindRouteCost(int start, int target)
    {
        searchId++;
        heapCount = 0;
        SetDistance(start, 0);
        Push(start, 0);

        while (heapCount > 0)
        {
            Pop(out var node, out var cost);
            if (GetDistance(node) != cost)
            {
                continue;
            }

            if (node == target)
            {
                return cost;
            }

            var x = node % Width;
            var z = node / Width;
            if (x > 0) Visit(node - 1, cost + 1);
            if (x < Width - 1) Visit(node + 1, cost + 1);
            if (z > 0) Visit(node - Width, cost + 1);
            if (z < Width - 1) Visit(node + Width, cost + 1);
        }

        return Width * 2;
    }

    private void Visit(int node, int cost)
    {
        if (IsBlocked(node) || GetDistance(node) <= cost)
        {
            return;
        }

        SetDistance(node, cost);
        Push(node, cost);
    }

    private int NextOpenCell(ref uint state)
    {
        do
        {
            state = unchecked(state * 1664525u + 1013904223u);
        }
        while (IsBlocked((int)(state % CellCount)));

        return (int)(state % CellCount);
    }

    private static bool IsBlocked(int node)
    {
        var x = node % Width;
        var z = node / Width;
        return (x * 17 + z * 31 + x * z) % 19 < 3;
    }

    private int GetDistance(int node)
    {
        return visitedAt[node] == searchId ? distances[node] : int.MaxValue;
    }

    private void SetDistance(int node, int distance)
    {
        visitedAt[node] = searchId;
        distances[node] = distance;
    }

    private void Push(int node, int cost)
    {
        var index = heapCount++;
        while (index > 0)
        {
            var parent = (index - 1) / 2;
            if (heapCosts[parent] <= cost)
            {
                break;
            }

            heapNodes[index] = heapNodes[parent];
            heapCosts[index] = heapCosts[parent];
            index = parent;
        }

        heapNodes[index] = node;
        heapCosts[index] = cost;
    }

    private void Pop(out int node, out int cost)
    {
        node = heapNodes[0];
        cost = heapCosts[0];
        heapCount--;
        if (heapCount == 0)
        {
            return;
        }

        var lastNode = heapNodes[heapCount];
        var lastCost = heapCosts[heapCount];
        var index = 0;
        while (true)
        {
            var left = index * 2 + 1;
            if (left >= heapCount)
            {
                break;
            }

            var right = left + 1;
            var child = right < heapCount && heapCosts[right] < heapCosts[left] ? right : left;
            if (heapCosts[child] >= lastCost)
            {
                break;
            }

            heapNodes[index] = heapNodes[child];
            heapCosts[index] = heapCosts[child];
            index = child;
        }

        heapNodes[index] = lastNode;
        heapCosts[index] = lastCost;
    }
}
