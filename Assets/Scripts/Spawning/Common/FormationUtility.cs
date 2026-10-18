using System.Collections.Generic;
using UnityEngine;

public enum FormationShape
{
    Wedge,
    Line,
    Column,
    Box
}

public static class FormationUtility
{
    // Returns local offsets (x = right, z = forward). Index 0 is always the leader at (0,0,0).
    public static List<Vector3> GetOffsets(FormationShape shape, int count, float spacing)
    {
        var offsets = new List<Vector3>(count);

        for (int i = 0; i < count; i++)
        {
            switch (shape)
            {
                case FormationShape.Line:
                    offsets.Add(new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f));
                    break;

                case FormationShape.Column:
                    offsets.Add(new Vector3(0f, 0f, -i * spacing));
                    break;

                case FormationShape.Box:
                    {
                        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
                        int row = i / columns;
                        int column = i % columns;
                        offsets.Add(new Vector3((column - (columns - 1) * 0.5f) * spacing, 0f, -row * spacing));
                        break;
                    }

                default: // Wedge: leader in front, wingmen alternate right/left and step back each row.
                    {
                        if (i == 0)
                        {
                            offsets.Add(Vector3.zero);
                            break;
                        }
                        int row = (i + 1) / 2;
                        float side = (i % 2 == 1) ? 1f : -1f;
                        offsets.Add(new Vector3(side * row * spacing, 0f, -row * spacing));
                        break;
                    }
            }
        }

        return offsets;
    }

    // Points on an arc around a centre, all at the same radius. Slot 0 is in the middle (on the facing direction),
    // then alternating right/left outwards. arcDegrees >= 360 gives an even ring covering every angle.
    public static List<Vector3> GetArcPositions(Vector3 centre, Vector3 facing, float radius, float arcDegrees, int count)
    {
        var positions = new List<Vector3>(count);
        if (count <= 0) return positions;

        facing.y = 0f;
        facing = facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;

        float step = arcDegrees >= 360f ? 360f / count
                   : count > 1 ? arcDegrees / (count - 1)
                   : 0f;

        for (int i = 0; i < count; i++)
        {
            int ring = (i + 1) / 2;
            float side = (i % 2 == 1) ? 1f : -1f;
            float angle = i == 0 ? 0f : side * ring * step;
            positions.Add(centre + Quaternion.Euler(0f, angle, 0f) * facing * radius);
        }

        return positions;
    }

    // Converts local offsets into world positions around an anchor, facing a direction on the XZ plane.
    public static List<Vector3> GetWorldPositions(FormationShape shape, int count, float spacing, Vector3 anchor, Vector3 facing)
    {
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;

        Quaternion rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        List<Vector3> offsets = GetOffsets(shape, count, spacing);

        for (int i = 0; i < offsets.Count; i++)
        {
            offsets[i] = anchor + rotation * offsets[i];
        }

        return offsets;
    }
}
