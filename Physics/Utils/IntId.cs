using System;
using System.Collections.Generic;

namespace Physics.Utils;

public class IntId
{
    private int _nextId = 0;
	private readonly Stack<int> _freeIds = [];

    public int GetNextId()
    {
        if (_freeIds.Count > 0)
            return _freeIds.Pop();
        if (_nextId == int.MaxValue)
            throw new InvalidOperationException("ID space exhausted");
        return _nextId++;
    }

    public void ReleaseId(int id)
    {
        _freeIds.Push(id);
    }
}
