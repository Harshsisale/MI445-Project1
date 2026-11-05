using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class WorldRecipeInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite recipeImage;

    private RecipeViewer viewer;

    public string Prompt => "Read recipe";

    public void Interact(PlayerItemInteraction player)
    {
        if (player == null || recipeImage == null)
            return;

        viewer = player.GetComponent<RecipeViewer>();
        if (viewer == null)
            viewer = player.gameObject.AddComponent<RecipeViewer>();

        viewer.Open(this, recipeImage);
    }

    private void OnDisable()
    {
        if (viewer != null)
            viewer.Close(this);
    }
}
