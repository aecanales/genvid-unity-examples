# 04 - Events
Basic example that shows how to send player input from the web view to the game via events. Clicking on the web view will plant a tree on the clicked location. 

![Event examples](../img/04.gif)

## Relevant Configuration Files
* **GenvidServices/config/events.json:** Contains the definition of event we send from the web view to the game.

## Important Files
* **SampleScene:** Scene with simple tiled background a "GenvidSessionManager" prefab added and correctly set-up. The "GenvidEvents" child object has been configured to handle a event named "click" as defined in the `TreeEvent.cs` component.
* **TreeEvent.cs:** Script that recieves the click event and creates the tree at the correct location.

## Relevant Web View Code
We recieve the click event and send it to Unity via ` this.client.sendEventObject()`.
```typescript
// Returns the click relative to the mouseOverlay div (instead of relative to the entire DOM).
const relativeClickPosition = (click) => {
    const rect = this.mouseOverlay.getBoundingClientRect();
    return {X: click.pageX - rect.x, Y: click.pageY - rect.y}                
}

this.mouseOverlay.addEventListener("click", (event) => {
    const clickObject = relativeClickPosition(event);
    this.client.sendEventObject({'click': `${clickObject.X},${clickObject.Y}`});
});
```