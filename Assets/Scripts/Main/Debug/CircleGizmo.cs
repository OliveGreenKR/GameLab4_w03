using UnityEngine;

public class CircleGizmo : MonoBehaviour
{
    [SerializeField]
    private bool showGizmo = true;

    [SerializeField]
    private Color gizmoColor = Color.yellow;

    [SerializeField]
    [Range(0.1f, 10f)]
    private float gizmoRadius = 1f;

    /// <summary>
    /// This method is called by Unity when the object is selected in the Editor.
    /// It's used to draw Gizmos for debugging purposes.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo)
        {
            return;
        }

        // Set the color for the Gizmo
        Gizmos.color = gizmoColor;

        // Draw the wire sphere (a circle from a top-down view)
        // The center is the object's position, and the radius is the serialized value.
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}