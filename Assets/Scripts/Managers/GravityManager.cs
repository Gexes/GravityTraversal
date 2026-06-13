using System.Collections.Generic;
using UnityEngine;

public class GravityManager : MonoBehaviour
{
    private static GravityManager instance;
    private List<PlanetGravity> activePlanets = new List<PlanetGravity>();

    void Awake()
    {
        instance = this;
        // Automatically find every planet inside your level hierarchy
        activePlanets.AddRange(FindObjectsByType<PlanetGravity>(FindObjectsSortMode.None));
    }

    public static PlanetGravity GetNearestPlanet(Vector3 playerPosition)
    {
        if (instance == null || instance.activePlanets.Count == 0) return null;

        PlanetGravity bestPlanet = null;
        float closestDistance = float.MaxValue;

        foreach (PlanetGravity planet in instance.activePlanets)
        {
            if (planet.IsPositionInField(playerPosition))
            {
                float distance = Vector3.Distance(playerPosition, planet.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestPlanet = planet;
                }
            }
        }

        // Fallback: If player flies out into deep outer space, default to the closest planet anyway
        if (bestPlanet == null)
        {
            float absoluteClosest = float.MaxValue;
            foreach (PlanetGravity planet in instance.activePlanets)
            {
                float dist = Vector3.Distance(playerPosition, planet.transform.position);
                if (dist < absoluteClosest)
                {
                    absoluteClosest = dist;
                    bestPlanet = planet;
                }
            }
        }

        return bestPlanet;
    }
}
