using System.Collections.Generic;
using UnityEngine;

public class RoomDecorGenerator
{
    private DecorationField[] fields;
    private Dictionary<FieldType, Decoration> decorations;

    public RoomDecorGenerator(
        DecorationField[] fields,
        Dictionary<FieldType, Decoration> decorations)
    {
        this.fields = fields;
        this.decorations = decorations;
    }
    
    public void AddDecoration(int index, Decoration decoration)
    {
        
    }
}
