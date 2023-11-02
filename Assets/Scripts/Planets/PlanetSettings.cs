using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName = "Planet Body/Settings Holder")]
public class PlanetSettings : ScriptableObject {
    public ShapeSettings shape;
    public ShadingSettings shading;
}