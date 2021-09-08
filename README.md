# genvid-unity-examples
Collection of examples developed using the Genvid SDK for Unity.

## How to run
Make sure to have the [Genvid SDK](https://www.youtube.com/watch?v=3UMQcn3uLIw) installed. Clone or download the repository, and open a command prompt in the `XX - EXAMPLE\GenvidServices` folder. Run the following commands to start the Genvid service:
```
genvid-bastion install --bastionid localbastion --loadconfig
genvid-sdk setup
genvid-sdk clean-config
genvid-sdk load-config-sdk
py .\genvid-project.py load
```
Once the previous commands have completed, open a command prompt in the `XX - EXAMPLE\GenvidServices\web` folder. Run the following command to build the web view:
```
py build.py all 
```
Now open the project in Unity. Open the Genvid window from the "Windows" menu in the top bar. Give it a second to load and then select your "local" Genvid cluster from the dropdown. To run the project, hit the "On" button besides the "services" and "web" jobs. Wait for them to initialize, and then press the Play button in the Unity Editor to start the game. Make sure the "Game" view resolution is set to 1280x720 for the stream to look correctly. Press the "Genvid Tanks Demo Open Link" to open the web view in your browser. 

## Examples
### 01 - Data Streams 
Basic examples that contains a rotating cube. Demostrates how to set up a scene and send data about the cube to a Genvid stream via data streams, to then display them in web view.

![Data Stream example](./img/01.gif)

### 02 - Web View Interaction
Example of how to add interaction to the web view based on information sent from Unity. The sample displays a map and sends a data stream with the bounding box corresponding to each location. The web view user can click on one of these locations to see detailed information.

![Web view interaction example](./img/02.gif)

### 03 - Annotations and Notifications
Example showing how to use annotations and notifications and illustrates the difference between them.  Check out the corresponding readme for the explanation.

![Annotations and Notifications example](./img/03.gif)

### 04 - Events
Basic example that shows how to send player input from the web view to the game via events. Clicking on the web view will plant a tree on the clicked location. 

![Events example](./img/04.gif)

### 05 - Events and Commands
Example that shows how to send player input from the web view to the game via events and commands.
![Events and Commands example](./img/05.gif)

### 06 - Chess Game
Complex example that shows a chess game against an AI where players can vote for the moves White will execute. Built upon [SebLague's Chess AI](https://github.com/SebLague/Chess-AI) for Unity.

![Chess game example](./img/06.gif)