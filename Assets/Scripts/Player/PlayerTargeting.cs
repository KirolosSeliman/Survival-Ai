using UnityEngine;

public static class PlayerTargeting
{
    public static Transform FindNearestWithTag(Vector3 origin, string tag, float maxDistance)
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag(tag);
        Transform best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] == null) continue;
            float sqr = (objs[i].transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = objs[i].transform;
            }
        }

        return best;
    }

    public static Vector2 LocalXZ(Transform frame, Vector3 worldDelta)
    {
        Vector3 local = frame.InverseTransformDirection(worldDelta);
        return new Vector2(local.x, local.z);
    }

    public static float Norm01(float d, float maxD)
    {
        if (maxD <= 0.0001f) return 0f;
        return Mathf.Clamp01(d / maxD);
    }
}
