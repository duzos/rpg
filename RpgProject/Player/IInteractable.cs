using Microsoft.Xna.Framework;

namespace RpgProject.Player;

public interface IInteractable {
    Rectangle InteractArea { get; }
    void OnInteract(Character player);
}
