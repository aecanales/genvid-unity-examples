// Copyright 2016-2020 Genvid Technologies Inc. All Rights Reserved.
import * as genvid from "genvid";
import * as genvidMath from "genvid-math";

namespace unityTankSample {

    // ------------------------------------------------class/interface defines------------------------------------------------
    export interface BoundingBox {
        X: number;
        Y: number;
        Width: number;
        Height: number;
        ID: number;
    }

    interface ICommandRequest {
        id: string;
        value: string;
    }    
    
    // Conversion from Json data into structure for a specific tank
    export interface ITankData {
        id: number;
        matrix: IUnityMatrix4x4;
        health: number;
        shotsFired: number;
        shotsHit: number;
        name: string;
        color: IUnityColor;
        cheers: number;
        activeBuff: number;
        roundWins: number;
    }

    // Conversion from Json data into structure for a low refresh rate game state
    export interface IMatchStateData {
        stateID: number;
        roundNumber: number;
        winningTankId: number;
    }

    // Conversion from Json data into structure for a high refresh rate game state
    export interface IGameData {
        matProjView: IUnityMatrix4x4;
        tanks: ITankData[];
        adBillboardMatrix: IUnityMatrix4x4;
        lootCountdown: number;
        lootMatrices: IUnityMatrix4x4[];
        shellMatrices: IUnityMatrix4x4[];
        explosionMatrices: IUnityMatrix4x4[];
    }

    // Conversion from Json data into structure for votes.  array index indicates loot type
    export interface ILootVotesData {
        lootVotes: number[];
    }

    // Conversion from Json data into structure for unity matrix
    export interface IUnityMatrix4x4 {
        e00: number;
        e01: number;
        e02: number;
        e03: number;
        e10: number;
        e11: number;
        e12: number;
        e13: number;
        e20: number;
        e21: number;
        e22: number;
        e23: number;
        e30: number;
        e31: number;
        e32: number;
        e33: number;
    }

    // Conversion from Json data into structure for unity color
    export interface IUnityColor {
        r: number;
        g: number;
        b: number;
        a: number;
    }

    // WebGL class for rendering command
    class RenderCommand {
        visible: boolean;
        vtx: [WebGLBuffer, number]; // Vertex buffer and length
        idx: [WebGLBuffer, number]; // Index buffer and length
        tex: any; // Texture ID.
        img: any; // HTML image used for the texture (async load).
        mat: number[];
    }

    // unityController used fo all the methods on the page
    export class UnityController {
        bugs: Array<BoundingBox>;

        client: genvid.IGenvidClient;
        streamInfo: genvid.IStreamInfo;
        video_player: genvid.IVideoPlayer = null;
        currentData: genvid.IDataFrame = null;

        timeLocalDiv: HTMLDivElement;
        timeVideoDiv: HTMLDivElement;
        timeVideoRawDiv: HTMLDivElement;
        timeComposeDiv: HTMLDivElement;
        timeComposeLastDiv: HTMLDivElement;
        timeStreamDiv: HTMLDivElement;
        latencyDiv: HTMLDivElement;
        delayOffsetDiv: HTMLDivElement;
        controlsDiv: HTMLDivElement;
        timeCamSceneDiv: HTMLDivElement;
        fullScreenDiv: HTMLDivElement;
        fullScreenElement: HTMLElement;

        tankCheerDiv: HTMLDivElement[] = [];

        help_overlay: HTMLDivElement;
        genvidOverlayButton: HTMLLinkElement;
        genvidOverlay: HTMLDivElement;
        promptOverlay: HTMLDivElement;
        helpButton: HTMLLinkElement;

        timeVisiblePrompt: number = 100;
        timeVisibleMax: number = 100;
        volumeChange: number = 0;

        circleRadius: number = 4;

        videoReady: boolean = false;
        playerTableSetupCompletion: boolean = false;
        lastGameData: IGameData = null;
        lastMatchData: IMatchStateData = null;

        latestLootVoteData: ILootVotesData;

        videoOverlay: HTMLDivElement;
        mapOverlay: HTMLDivElement;
        canvas3d: HTMLCanvasElement;
        canvas2d: HTMLCanvasElement;
        genvidWebGL: genvid.IWebGLContext;
        context2d: CanvasRenderingContext2D;
        lastSelection: number;

        mouseOverlay: HTMLDivElement;

        mapImage: HTMLImageElement;
        tankImage: HTMLImageElement;
        shellImage: HTMLImageElement;
        lootImage: HTMLImageElement;
        impactImage: HTMLImageElement;

        tankData: ITankData[];

        blueCheers: number = 0;
        redCheers: number = 0;
        blueCheerTime: number = 0;
        redCheerTime: number = 0;

        lootMatrices: IUnityMatrix4x4[];
        shellMatrices: IUnityMatrix4x4[];
        explosionMatrices: IUnityMatrix4x4[];

        // WebGL variables.
        gfx_prog: any; // The shader program.
        gfx_prog_loc_viewproj: any;
        gfx_prog_loc_world: any;

        gfx_prog_data_viewproj: number[] = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]; // identity matrix
        gfx_cmd_grid: RenderCommand;
        gfx_cmd_selection: RenderCommand;
        gfx_cmd_ad: RenderCommand;

        // Time when the broadcast started (matches what is present in game data).
        last_game_time_received: number = -1;

        isFullScreen: boolean = false;

        // Callbacks
        private on_video_ready_callback = null;

        constructor(private video_player_id: string) {
        }

        // ---------------------------------------------------------Genvid Client initialization section---------------------------------------------------------
        // Start the connection to the services
        onConnect() {
            let promise = $.post("/api/public/channels/join", {}, (joinRep) => {
                this.on_channel_join(<genvid.IChannelJoinResponse>joinRep);
            });
            promise.fail((err) => {
                alert("Can't get the stream info:" + err);
            });
        }

        onMute() {
            const muteIcon = document.querySelector("#mute-button i");
            this.promptOverlay = <HTMLDivElement>document.querySelector("#prompt_overlay");
            if (this.client.videoPlayer.getMuted()) {
                muteIcon.classList.remove("fa-volume-off");
                muteIcon.classList.add("fa-volume-up");
                this.client.videoPlayer.setMuted(false);
                this.promptOverlay.style.visibility = "visible";
                this.promptOverlay.textContent = "Volume is unmuted";
                this.timeVisiblePrompt = 0;
            } else {
                muteIcon.classList.remove("fa-volume-up");
                muteIcon.classList.add("fa-volume-off");
                this.client.videoPlayer.setMuted(true);
                this.promptOverlay.style.visibility = "visible";
                this.promptOverlay.textContent = "Volume is muted";
                this.timeVisiblePrompt = 0;
            }
        }

        // Create the genvid Client and the function listening to it
        private on_channel_join(joinRep: genvid.IChannelJoinResponse) {
            this.streamInfo = joinRep.info;
            this.client = genvid.createGenvidClient(this.streamInfo, joinRep.uri, joinRep.token, this.video_player_id);
            this.client.onVideoPlayerReady((elem) => { this.on_video_player_ready(elem); });
            this.client.onStreamsReceived((streams) => { this.on_streams_received(streams); });
            this.client.onNotificationsReceived(this.on_notifications_received.bind(this));
            this.client.onDraw((frame) => { this.on_new_frame(frame); });
            this.client.start();
        }

        // Once the video player is ready, get ready the other component
        private on_video_player_ready(_elem: HTMLElement) {
            // variables init
            this.video_player = this.client.videoPlayer;

            this.timeLocalDiv = <HTMLDivElement>document.querySelector("#time_local");
            this.timeVideoDiv = <HTMLDivElement>document.querySelector("#time_video");
            this.timeVideoRawDiv = <HTMLDivElement>document.querySelector("#time_video_raw");
            this.timeComposeDiv = <HTMLDivElement>document.querySelector("#time_compose");
            this.timeComposeLastDiv = <HTMLDivElement>document.querySelector("#time_compose_last");
            this.timeStreamDiv = <HTMLDivElement>document.querySelector("#time_stream");
            this.latencyDiv = <HTMLDivElement>document.querySelector("#latency");
            this.delayOffsetDiv = <HTMLDivElement>document.querySelector("#delay_offset");
            this.controlsDiv = <HTMLDivElement>document.querySelector("#game-controls");

            this.timeCamSceneDiv = <HTMLDivElement>document.querySelector("#timeCamScene_overlay");

            this.promptOverlay = <HTMLDivElement>document.querySelector("#prompt_overlay");

            this.videoOverlay = <HTMLDivElement>document.querySelector("#video_overlay");
            this.mapOverlay = <HTMLDivElement>document.querySelector("#map_overlay");
            this.canvas3d = <HTMLCanvasElement>document.querySelector("#canvas_overlay_3d");
            this.canvas2d = <HTMLCanvasElement>document.querySelector("#canvas_overlay_2d");

            this.mouseOverlay = <HTMLDivElement>document.querySelector("#mouse_overlay");

            this.genvidOverlayButton = <HTMLLinkElement>document.querySelector("#genvid_overlay_button");
            this.genvidOverlay = <HTMLDivElement>document.querySelector("#genvid_overlay");
            this.help_overlay = <HTMLDivElement>document.querySelector("#help_overlay");
            this.helpButton = <HTMLLinkElement>document.querySelector("#help_button");
            this.fullScreenDiv = <HTMLDivElement>document.querySelector(".fullscreen-button");
            this.fullScreenElement = <HTMLElement>document.querySelector(".fa-expand");

            // canvas context setup
            this.genvidWebGL = genvid.createWebGLContext(this.canvas3d); // Need to assign before any resize.
            this.context2d = this.canvas2d.getContext('2d');

            // map image caching
            this.mapImage = new Image();
            this.mapImage.src = "img/map/mapBG.png";
            this.tankImage = new Image();
            this.tankImage.src = "img/map/tank.png";
            this.shellImage = new Image();
            this.shellImage.src = "img/map/shell.png";
            this.impactImage = new Image();
            this.impactImage.src = "img/map/impact.png";     
            this.lootImage = new Image();
            this.lootImage.src = "img/map/loot.png";         

            // events
            this.hideOverlay();
            this.client.videoPlayer.addEventListener(genvid.PlayerEvents.PAUSE, () => {
                this.hideOverlay();
            });

            this.client.videoPlayer.addEventListener(genvid.PlayerEvents.PLAYING, () => {
                this.showOverlay();
            });

            if (this.on_video_ready_callback) {
                this.on_video_ready_callback();
            }

            this.mouseOverlay.addEventListener("click", (_event) => { this.setSelection(0); }, false);

            document.addEventListener("fullscreenchange", () => { this.onResize(); });
            document.addEventListener("webkitfullscreenchange", () => { this.onResize(); });
            document.addEventListener("mozfullscreenchange", () => { this.onResize(); });
            _elem.addEventListener("resize", () => { this.onResize(); });

            window.addEventListener("resize", () => { this.onResize(); }, true);
            window.addEventListener("orientationchange", () => { this.onResize(); }, true);
            window.addEventListener("sizemodechange", () => { this.onResize(); }, true);
            window.setInterval(() => { this.onResize(); }, 1000); // Just a safety, in case something goes wrong.

            this.genvidOverlayButton.addEventListener("click", (_event) => { this.toggleGenvidOverlay(); }, false);
            this.helpButton.addEventListener("click", (_event) => { this.onHelpActivation(); }, false);
            this.fullScreenDiv.addEventListener("click", (_event) => { this.toggleFullScreen(); }, false);

            const muteIcon = document.getElementById("mute-button");
            muteIcon.addEventListener("click", () => this.onMute());
            if (!this.video_player.getMuted()) {
                this.onMute();
            }

            const isInBounds = (spot, click) => {
                return spot.X <= click.X && click.X <= spot.X + spot.Width &&
                       spot.Y <= click.Y && click.Y <= spot.Y + spot.Height;
            };
            
            // Returns the click relative to the mouseOverlay div (instead of relative to the entire DOM).
            const relativeClickPosition = (click) => {
                const rect = this.mouseOverlay.getBoundingClientRect();
                return {X: click.pageX - rect.x, Y: click.pageY - rect.y}
            };
            
            this.mouseOverlay.addEventListener("click", (event) => { 
                if (this.bugs) {
                    this.bugs.forEach(bug => {
                        if (isInBounds(bug, relativeClickPosition(event))) {
                            console.log(`Clicked on ${bug.ID}`);
                            this.client.sendEventObject({'click': bug.ID});
                        }
                    });
                }
            });

            let restartButton = <HTMLButtonElement>document.querySelector("#restart_game_button");
            restartButton.addEventListener("click", (_event) => { 
                let command: ICommandRequest = {
                    id: "RestartMatch",
                    value: "null"
                }; 
                
                let promise = $.post("/api/admin/commands/game", command).then(() => {
                    console.log("Game has been restarted.");
                });
    
                promise.fail((err) => {
                    console.error(`Failed to send command: ${err}`)
                });
            }, false);

            /*
            let mineButton = <HTMLButtonElement>document.querySelector("#VoteMine");
            mineButton.addEventListener("click", (_event) => { this.onVote(0); }, false);

            let healthButton = <HTMLButtonElement>document.querySelector("#VoteHealth");
            healthButton.addEventListener("click", (_event) => { this.onVote(1); }, false);

            let shieldButton = <HTMLButtonElement>document.querySelector("#VoteShield");
            shieldButton.addEventListener("click", (_event) => { this.onVote(4); }, false);

            let movementButton = <HTMLButtonElement>document.querySelector("#VoteMovement");
            movementButton.addEventListener("click", (_event) => { this.onVote(3); }, false);

            let attackButton = <HTMLButtonElement>document.querySelector("#VoteAttack");
            attackButton.addEventListener("click", (_event) => { this.onVote(2); }, false);

            let mapButton = <HTMLButtonElement>document.querySelector("#ToggleMap");
            mapButton.addEventListener("click", (_event) => { this.onToggleMap(); }, false);
            */

            // Initialize graphics stuff.
            this.genvidWebGL.clear();
            let gl = this.genvidWebGL.gl;
            gl.disable(gl.DEPTH_TEST);

            this.gfx_initShaders();
            this.gfx_initRenderCommands();

            // kick off map update
            window.requestAnimationFrame(this.mapUpdate);
            
            this.onResize();
            this.videoReady = true;
        }

        showOverlay() {
            this.genvidOverlay.style.display = "block";
        }

        hideOverlay() {
            this.genvidOverlay.style.display = "none";
        }

        public on_video_ready(callback) {
            this.on_video_ready_callback = callback;
        }

        // ---------------------------------------------------------Enter frame section---------------------------------------------------------
        private on_new_frame(frameSource: genvid.IDataFrame) {
            this.bugs = JSON.parse(frameSource.streams.bugs.data).BoundingBoxes;

            let gameDataFrame = frameSource.streams["GameData"];
            let gameData: IGameData = null;            
            if (gameDataFrame && gameDataFrame.user) {
                gameData = gameDataFrame.user;
                let tanks = gameData.tanks;
                this.tankData = tanks;

                this.explosionMatrices = gameData.explosionMatrices;
                this.lootMatrices = gameData.lootMatrices;
                this.shellMatrices = gameData.shellMatrices;

                let matchDataFrameAnnotations = frameSource.annotations["MatchState"];
                if (matchDataFrameAnnotations) {
                    for (let annotation of matchDataFrameAnnotations) {
                        let matchState = <IMatchStateData>annotation.user;
                        this.refreshMatchState(matchState, tanks);
                    }
                }

                // Setup the player table once the game data is ready
                if (this.playerTableSetupCompletion === false) {
                    this.playerTableSetupCompletion = true;
                    this.initPlayerTable(tanks);
                }

                let mat = gameData.matProjView;
                this.gfx_prog_data_viewproj = [mat.e00, mat.e10, mat.e20, mat.e30, mat.e01, mat.e11, mat.e21, mat.e31, mat.e02, mat.e12, mat.e22, mat.e32, mat.e03, mat.e13, mat.e23, mat.e33];
                
                this.lootDropUpdate(gameData.lootCountdown);
                this.tankOverlayUpdate(tanks, gameData.matProjView);
                this.adOverlayUpdate(gameData.adBillboardMatrix);
                this.gfx_draw3D();
            }

            if (this.videoReady) {
                this.genvidInformationOverlayUpdate(); // Update the Genvid information overlay
                this.visibilityUpdate(); // Update the visibility on the overlay when using key press
            }

            let isFullScreen = this.checkFullScreen();
            if (isFullScreen !== this.isFullScreen) {
                this.isFullScreen = isFullScreen;
            }
        }

        private StringForTank(tank:ITankData, msg:string): string {
            var color:string = "rgb("+ (tank.color.r * 255) + ","+ (tank.color.g * 255) + ","+ (tank.color.b * 255) + ")";
            return "<span style='color:" + color + "'>" + tank.name + "</span>" + msg;
        }

        private refreshMatchState(matchData: IMatchStateData, tanks: ITankData[]) {
            let roundInfo: HTMLElement = <HTMLDivElement>document.querySelector("#roundInfo");
            let topText: HTMLElement = <HTMLDivElement>document.querySelector("#topText");
            let score1: HTMLElement = <HTMLDivElement>document.querySelector("#score1");
            let score2: HTMLElement = <HTMLDivElement>document.querySelector("#score2");

            if (matchData.stateID == 0) {
                // display round number
                roundInfo.style.display = "block";
                topText.textContent = "Round " + matchData.roundNumber;
                score1.style.display = "none";
                score2.style.display = "none";
            } else if (matchData.stateID == 1) {
                // update tank stuff
                roundInfo.style.display = "none";
            } else if (matchData.stateID == 2) {
                // display winner and scores
                roundInfo.style.display = "block";

                this.setSelection(0); // unselect tank

                var winningTank:ITankData = null;

                if (matchData.winningTankId != 0) winningTank = tanks[matchData.winningTankId - 1];

                if (winningTank != null && winningTank.roundWins == 5) {
                    topText.innerHTML = this.StringForTank(winningTank, " Wins the Match!");
                    score1.style.display = "none";
                    score2.style.display = "none";
                } else {
                    var winText:string;
                    if (winningTank == null) winText = "Draw";
                    else winText = this.StringForTank(winningTank, " Wins the Round!");

                    topText.innerHTML = winText;                   
                    score1.innerHTML = this.StringForTank(tanks[0], ": " + tanks[0].roundWins + (tanks[0].roundWins == 1 ? " Win" : " Wins"));
                    score2.innerHTML = this.StringForTank(tanks[1], ": " + tanks[1].roundWins + (tanks[1].roundWins == 1 ? " Win" : " Wins"));
                    score1.style.display = "block";
                    score2.style.display = "block";
                }
            }
        }

        private lootDropUpdate(lootCountdown: number) {
            if (lootCountdown > 0) {
                let elemTimer: HTMLElement = <HTMLDivElement>document.querySelector("#lootDropTimer");
                var time = Math.round(lootCountdown);
                elemTimer.textContent = "Incoming loot drop! " + time;
                elemTimer.style.visibility = "visible";
                elemTimer.style.display = "block";
            } else {
                let elemTimer: HTMLElement = <HTMLDivElement>document.querySelector("#lootDropTimer");
                elemTimer.style.visibility = "hidden";
                elemTimer.style.display = "none";
            }
            if (lootCountdown * 1000 > this.client.streamLatencyMS) {

                let elemTimer: HTMLElement = <HTMLDivElement>document.querySelector("#lootDropTimer");
                var time = Math.round(lootCountdown - this.client.streamLatencyMS / 1000);
                elemTimer.textContent = "Vote for the next loot drop! " + time;

                if (this.latestLootVoteData) {
                    let elemMine: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultMine");
                    elemMine.textContent = "Mine Votes: " + this.latestLootVoteData.lootVotes[0];
                    let elemHealth: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultHealth");
                    elemHealth.textContent = "Health Votes: " + this.latestLootVoteData.lootVotes[1];
                    let elemAttack: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultAttack");
                    elemAttack.textContent = "Attack Votes: " + this.latestLootVoteData.lootVotes[2];
                    let elemMovement: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultMovement");
                    elemMovement.textContent = "Mobility Votes: " + this.latestLootVoteData.lootVotes[3];
                    let elemShield: HTMLElement = <HTMLDivElement>document.querySelector("#voteResultShield");
                    elemShield.textContent = "Shield Votes: " + this.latestLootVoteData.lootVotes[4];
                }
                let elemVoteInfoPanel: HTMLElement = <HTMLDivElement>document.querySelector("#voteInfoPanel");
                elemVoteInfoPanel.style.visibility = "visible";
                elemVoteInfoPanel.style.display = "block";
            } else {
                let elemVoteInfoPanel: HTMLElement = <HTMLDivElement>document.querySelector("#voteInfoPanel");
                elemVoteInfoPanel.style.visibility = "hidden";
                elemVoteInfoPanel.style.display = "none";
            }
        }

        private tankOverlayUpdate(tanks: ITankData[], matProjView: IUnityMatrix4x4) {
            if (tanks) {
                let vertices: number[] = [];
                for (let tank of tanks) {
                    let m = tank.matrix;
                    let p = genvidMath.vec3(m.e03, m.e13, m.e23);
                    // Perform the webgl update process -- Update 3d
                    if (this.isSelected(0)) {                    
                        // don't display
                        let cmd: RenderCommand = this.gfx_cmd_selection;
                        cmd.mat = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
                    } else if (this.isSelected(tank.id)) {
                        let r = this.circleRadius;
                        let c = genvidMath.vec4(1, 0, 0, 1);
                        this.makeCircleY(vertices, 0, 0, 0, r, c);
                        let num_quads = vertices.length / (4 * 9);
                        let genvidWebGL = this.genvidWebGL;
                        let cmd: RenderCommand = this.gfx_cmd_selection;
                        cmd.vtx = genvidWebGL.createBuffer(new Float32Array(vertices));
                        cmd.idx = genvidWebGL.createIndexBufferForQuads(num_quads);
                        cmd.mat = [1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, m.e03, m.e13, m.e23, 1];
                    }

                    // Move the name tag of the tank
                    let tag = this.findOrCreateTagDiv(tank);
                    let mat = this.convertMatrix(matProjView);
                    let pos_2d = genvidMath.projectPosition(mat, p);
                    this.center_at(tag, pos_2d, genvidMath.vec2(0, -50));

                    // move the cheer overlay to the tank
                    let cheer = this.findOrCreateCheerDiv(tank);
                    this.center_at(cheer, pos_2d, genvidMath.vec2(0, -50));

                    // Move the details to the tank
                    let details = this.findOrCreateDetailsDiv(tank);
                    this.center_at(details, pos_2d, genvidMath.vec2(0, 150));
                }
            }
        }

        private adOverlayUpdate(m: IUnityMatrix4x4) {
            // setup the ad
            let vertices: number[] = [];
            let size = 1.1; // radius

            this.SetupVertexBuffer(vertices, size);
            let num_quads = vertices.length / (4 * 9);
            let genvidWebGL = this.genvidWebGL;
            let cmd: RenderCommand = this.gfx_cmd_ad;
            cmd.vtx = genvidWebGL.createBuffer(new Float32Array(vertices));
            cmd.idx = genvidWebGL.createIndexBufferForQuads(num_quads);
            cmd.mat = [m.e00, m.e10, m.e20, m.e30, m.e01, m.e11, m.e21, m.e31, m.e02, m.e12, m.e22, m.e32, m.e03, m.e13, m.e23, m.e33];
        }

        private genvidInformationOverlayUpdate() {
            let w = 18; // Width of the content of every line (without label).
            let localTime: Date = new Date();
            this.timeLocalDiv.textContent = `Local: ${this.msToDuration(Math.round(localTime.getTime()))}`;

            let videoTimeRawMS = 0;
            let videoPlayer = this.client.videoPlayer;
            if (videoPlayer) videoTimeRawMS = videoPlayer.getCurrentTime() * 1000;

            this.timeVideoRawDiv.textContent = `Raw Video: ${this.preN(this.msToDuration(Math.round(videoTimeRawMS)), w)}`;
            this.timeVideoDiv.textContent = `Est. Video: ${this.preN(this.msToDuration(Math.round(this.client.videoTimeMS)), w)}`;
            this.timeComposeLastDiv.textContent = `Last Compose: ${this.preN(this.msToDuration(Math.round(this.client.lastComposeTimeMS)), w)}`;
            this.timeComposeDiv.textContent = `Est. Compose: ${this.preN(this.msToDuration(Math.round(this.client.composeTimeMS)), w)}`;
            this.timeStreamDiv.textContent = `Stream: ${this.preN(this.msToDuration(Math.round(this.client.streamTimeMS)), w)}`;
            this.latencyDiv.textContent = `Latency: ${this.preN(this.client.streamLatencyMS.toFixed(0), w - 3)} ms`;
            this.delayOffsetDiv.textContent = `DelayOffset: ${this.preN(this.client.delayOffset.toFixed(0), w - 3)} ms`;
        }

        private visibilityUpdate() {
            if (this.promptOverlay.style.visibility === "visible" && this.timeVisiblePrompt < this.timeVisibleMax) {
                this.timeVisiblePrompt++;
                if (this.volumeChange === 2) {
                    this.volumeChange = 0;
                    this.promptOverlay.textContent = "Volume: " + this.client.videoPlayer.getVolume().toString() + " %";
                } else if (this.volumeChange === 1) {
                    this.volumeChange = 0;
                    this.promptOverlay.textContent = "Volume: " + this.client.videoPlayer.getVolume().toString() + " %";
                }
            } else if (this.promptOverlay.style.visibility === "visible" && this.timeVisiblePrompt >= this.timeVisibleMax) {
                this.promptOverlay.style.visibility = "hidden";
            }
        }

        // creates clickable cheer buttons for tanks
        initPlayerTable(tankData: ITankData[]) {
            for (let tank of tankData) {
                let tankAdd = `<div class='clickable tank` + tank.id + `'>
                                    <div>
                                        <span class='tank_name clickable'>` + tank.name + `</span>
                                        <button class='cheer' id='tank` + tank.id + `_cheerbutton'><i class='icon_like' aria-hidden='true'></i></button>
                                        <span class='cheer_value tank` + tank.id + `_cheer'></span>
                                    </div>
                                </div>`;

                var panelId = "";
                if (tank.id == 1) {
                    panelId = "#leftTankPanel";
                } else {
                    panelId = "#rightTankPanel";
                }

                $(panelId).append(tankAdd);

                let cheerButton = <HTMLButtonElement>document.querySelector("#tank" + tank.id + "_cheerbutton");
                cheerButton.addEventListener("click", (_event) => { this.onCheer(tank.id); _event.stopPropagation(); }, false);

                let tankCheerDiv = <HTMLDivElement>document.querySelector(".tank" + tank.id);
                tankCheerDiv.addEventListener("click", (_event) => { this.setSelection(tank.id); _event.stopPropagation(); }, false);
                this.tankCheerDiv.push(tankCheerDiv);
            }
        }

        // Upon receving the stream, get the timecode and the data
        private on_streams_received(dataStreams: genvid.IDataStreams) {
            for (let stream of dataStreams.streams) {
                for (let frame of stream.frames) {
                    if (this.last_game_time_received < frame.timeCode) {
                        this.last_game_time_received = frame.timeCode;
                    }
                }
            }

            // Parse the JSON from each elements
            for (let stream of [...dataStreams.streams, ...dataStreams.annotations]) {
                for (let frame of stream.frames) {
                    try {
                        if (stream.id == "GameData") frame.user = <IGameData>JSON.parse(frame.data);
                        if (stream.id == "MatchState") frame.user = <IMatchStateData>JSON.parse(frame.data);
                    }
                    catch (err) {
                        console.info("invalid Json format for:" + frame.data + " with error :" + err);
                    }

                }
            }
        }

        // Upon receiving a notification, get the notification content
        private on_notifications_received(message: genvid.IDataNotifications) {
             for (let notification of message.notifications) {
                 if (notification.id === "LootVotesData") {
                    let datastr = genvid.UTF8ToString(notification.rawdata);
                    try {
                        // Get the latest loot drop data
                        let userData = <ILootVotesData>JSON.parse(datastr);
                        this.latestLootVoteData = userData;
                    }
                    catch (err) {
                        console.info("invalid Json format for:" + datastr + " with error :" + err);
                    }
                 }
             }
        }

        // ---------------------------------------------------------User interactions section---------------------------------------------------------
        onKeyDown(event: KeyboardEvent) {
            // Seems to be the most cross-platform way to determine keys:
            // Ref: https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code

            let code = event.code || this.getKeyCode(event);
            if (code === "Equal" || code === "NumpadAdd") {
                this.changeOffset(+1, event);
            } else if (code === "Minus" || code === "NumpadSubtract") {
                this.changeOffset(-1, event);
            } else if (code === "NumpadMultiply") {
                this.changeOffset(0, event);
            } else if (code === "KeyG") {
                this.toggleGenvidOverlay();
            }
            else if (code === "Space") {
                if (this.client.videoPlayer.isPaused()) {
                    this.client.videoPlayer.play();
                } else {
                    this.client.videoPlayer.pause();
                }
                event.preventDefault();
            }
            else if (code === "KeyM") {
                this.onMute();
            }
            else if (code === "KeyZ") {
                this.promptOverlay = <HTMLDivElement>document.querySelector("#prompt_overlay");
                this.client.videoPlayer.setVolume(this.client.videoPlayer.getVolume() - 20);
                this.promptOverlay.style.visibility = "visible";
                this.timeVisiblePrompt = 0;
                this.volumeChange = 2;
            }
            else if (code === "KeyX") {
                this.promptOverlay = <HTMLDivElement>document.querySelector("#prompt_overlay");
                this.client.videoPlayer.setVolume(this.client.videoPlayer.getVolume() + 20);
                this.promptOverlay.style.visibility = "visible";
                this.timeVisiblePrompt = 0;
                this.volumeChange = 1;
            }
            else if (code === "KeyH") {
                this.onHelpActivation();
            }
        }

        // Compatibility code for browsers (Safari) not having KeyboardEvent.code.
        getKeyCode(event: KeyboardEvent) {
            if (event.keyCode) {
                console.log(event.keyCode, event.code);
                if (65 <= event.keyCode && event.keyCode <= 90) {
                    return "Key" + String.fromCharCode(event.keyCode);
                } else {
                    switch (event.keyCode) {
                        case 13: return "Enter";
                        case 106: return "NumpadMultiply";
                        case 107: return "NumpadAdd";
                        case 109: return "NumpadSubtract";
                        case 110: return "NumpadDecimal";
                        case 111: return "NumpadDivide";
                        case 187: return "Equal";
                        case 188: return "Comma";
                        case 189: return "Minus";
                        case 190: return "Period";
                        case 222: return "Backquote";
                    }
                }
            }
        }

        // Allow to adjust the various overlay when resizing the windows - needed to see the PromptOverlay and Name moving div
        onResize() {
            let refElement = this.client.videoElem; // The element to match.
            let refElementSize = refElement ? genvidMath.vec2(refElement.clientWidth, refElement.clientHeight) : genvidMath.vec2(1280, 720);
            let refElementRatio = refElementSize.x / refElementSize.y;
            let videoRatio = this.client.videoAspectRatio;
            let pos: genvidMath.IVec2;
            let size: genvidMath.IVec2;
            if (videoRatio >= refElementRatio) {
                // Top+Bottom bars, fill width fully, shrink height.
                let ey = refElementSize.x / videoRatio;
                let dy = refElementSize.y - ey;
                // Center vertically.
                let y = dy * 0.5;
                pos = genvidMath.vec2(0, Math.round(y));
                size = genvidMath.vec2(refElementSize.x, Math.round(ey));
            } else {
                // Left+Right bars, fill height fully, shrink width.
                let ex = refElementSize.y * videoRatio;
                let dx = refElementSize.x - ex;
                // Center horizontally.
                let x = dx * 0.5;
                pos = genvidMath.vec2(Math.round(x), 0);
                size = genvidMath.vec2(Math.round(ex), refElementSize.y);
            }
            let style = this.videoOverlay.style;
            let cur_pos = genvidMath.vec2(parseInt(style.left), parseInt(style.top));
            let cur_size = genvidMath.vec2(parseInt(style.width), parseInt(style.height));

            if (!genvidMath.equal2D(cur_size, size, 0.9) || !genvidMath.equal2D(cur_pos, pos, 0.9)) {
                this.videoOverlay.style.left = pos.x + "px";
                this.videoOverlay.style.width = size.x + "px";
                this.videoOverlay.style.top = pos.y + "px";
                this.videoOverlay.style.height = size.y + "px";
                this.canvas3d.width = size.x;
                this.canvas3d.height = size.y;
                this.genvidWebGL.setViewport(0, 0, size.x, size.y); // Render in the whole area.
            }

            // adjust the map size
            let mapSize = size.y * 0.5;
            this.mapOverlay.style.width = mapSize + "px";
            this.mapOverlay.style.height = mapSize + "px";
            this.canvas2d.width = mapSize;
            this.canvas2d.height = mapSize;

            if (this.lastSelection != null) {
                this.setSelection(this.lastSelection);
            }
        }

        toggleFullScreen() {
            let doc = <any>document;
            if (this.checkFullScreen()) {
                if (doc.exitFullscreen) {
                    doc.exitFullscreen();
                } else if (doc.mozCancelFullScreen) {
                    doc.mozCancelFullScreen();
                } else if (doc.webkitExitFullscreen) {
                    doc.webkitExitFullscreen();
                } else if (doc.msExitFullscreen) {
                    doc.msExitFullscreen();
                }
                this.fullScreenElement.classList.remove("fa-compress");
                this.fullScreenElement.classList.add("fa-expand");
            } else {
                let element = <any>document.querySelector("#video_area");
                if (element.requestFullscreen) {
                    element.requestFullscreen();
                } else if (element.mozRequestFullScreen) {
                    element.mozRequestFullScreen();
                } else if (element.webkitRequestFullscreen) {
                    element.webkitRequestFullscreen();
                } else if (element.msRequestFullscreen) {
                    element.msRequestFullscreen();
                }
                this.fullScreenElement.classList.remove("fa-expand");
                this.fullScreenElement.classList.add("fa-compress");
            }
        }

        // Set the selection to a specific tank
        setSelection(id: number) {
            if (this.lastSelection > 0) {
                let elem_id: string = "tank_details_" + this.lastSelection;
                let elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + elem_id);
                if (elem != null) {
                    elem.style.visibility = "hidden";
                }
            }

            this.lastSelection = id;
            let elem_id: string = "tank_details_" + id;
            let elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + elem_id);
            if (elem != null) {
                elem.style.visibility = "visible";
            }
        }

        // Verify if the tank is selected
        isSelected(id: number) {
            return this.lastSelection == id;
        }

        // Function that changed the delay offset depending of the key pressed
        private changeOffset(direction, event) {
            if (direction !== 0) {
                let delayDelta = 100 * direction;
                if (event.altKey) delayDelta *= 10;
                if (event.shiftKey) delayDelta /= 2;
                let newDelayOffset = this.client.delayOffset + delayDelta;
                this.client.delayOffset = newDelayOffset;
            } else {
                this.client.delayOffset = 0;
            }
        }

        // Display or remove the Genvid Overlay
        private toggleGenvidOverlay() {
            if (this.genvidOverlay.getAttribute("data-isHidden")) {
                this.genvidOverlay.setAttribute("data-isHidden", "");
                this.genvidOverlay.style.visibility = "visible";
                this.genvidOverlayButton.classList.remove("disabled");
            } else {
                this.genvidOverlay.setAttribute("data-isHidden", "true");
                this.genvidOverlay.style.visibility = "hidden";
                this.genvidOverlayButton.classList.add("disabled");
            }
            // TODO: add more divs to disable?
        }

        // Display or remove the help overlay
        private onHelpActivation() {
            if (this.help_overlay.style.visibility === "visible") {
                this.help_overlay.style.visibility = "hidden";
            }
            else {
                this.help_overlay.style.visibility = "visible";
            }
        }

        // Upon cheering a tank
        private onCheer(tankId: number) {
            let localTime: Date = new Date();
            let time: number = Math.round(localTime.getTime());
            if (tankId == 1) {
                if (time > this.blueCheerTime + 2000) this.blueCheers = 0; // reset old counter if old data
                this.blueCheerTime = time + 2000;
                this.blueCheers++;
            } else if (tankId == 2) {
                if (time > this.redCheerTime + 2000) this.redCheers = 0; // reset old counter if old data
                this.redCheerTime = time + 2000;
                this.redCheers++;
            }
            this.client.sendEventObject({ cheer: tankId });
        }

        /*
        // upon voting for a drop
        private onVote(dropId: number) {
            this.client.sendEventObject({ dropVote: dropId });
            // TODO: disable buttons?
        }

        private onToggleMap() {
            if (this.mapOverlay.style.display == "block") {
                this.mapOverlay.style.display = "none"
            } else {
                this.mapOverlay.style.display = "block"
            }
        }
        */

        // ---------------------------------------------------------WebGL section---------------------------------------------------------
        // Function used to draw the WebGL 3d
        gfx_draw3D() {
            // gl setup
            // Perform the 3d draw process
            let genvidWebGL = this.genvidWebGL;
            let gl = genvidWebGL.gl;

            genvidWebGL.clear();

            let prog = this.gfx_prog;
            gl.useProgram(prog);

            // We prepare the program only once (good thing we have a single material).
            gl.enableVertexAttribArray(0);
            gl.enableVertexAttribArray(1);
            gl.enableVertexAttribArray(2);
            gl.activeTexture(gl.TEXTURE0);
            gl.uniform1i(gl.getUniformLocation(prog, "tex"), 0);

            if (this.gfx_prog_data_viewproj) {
                gl.uniformMatrix4fv(this.gfx_prog_loc_viewproj, false, this.gfx_prog_data_viewproj);
            }

            // Draw commands.
            let cmds: RenderCommand[] = [this.gfx_cmd_selection, this.gfx_cmd_ad];
            for (let cmd of cmds) {
                if (cmd.visible && cmd.vtx) {
                    gl.bindBuffer(gl.ARRAY_BUFFER, cmd.vtx[0]);
                    gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 9 * 4, 0 * 4); // Position.
                    gl.vertexAttribPointer(1, 2, gl.FLOAT, false, 9 * 4, 3 * 4); // TexCoord.
                    gl.vertexAttribPointer(2, 4, gl.FLOAT, false, 9 * 4, 5 * 4); // Color.
                    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, cmd.idx[0]);
                    gl.bindTexture(gl.TEXTURE_2D, cmd.tex);
                    if (cmd.mat != null) gl.uniformMatrix4fv(this.gfx_prog_loc_world, false, cmd.mat);
                    genvidWebGL.checkGLError();

                    gl.drawElements(gl.TRIANGLES, cmd.idx[1], gl.UNSIGNED_SHORT, 0);
                    genvidWebGL.checkGLError();
                }
            }
        }

        // Web GL initalization of shaders
        gfx_initShaders() {
            let genvidWebGL = this.genvidWebGL;

            let vShaderStr = [
                "uniform mat4 g_ViewProjMat;",
                "uniform mat4 g_mWorldMat;",
                "attribute vec3 g_Position;",
                "attribute vec2 g_TexCoord0;",
                "attribute vec4 g_Color0;",
                "varying vec2 texCoord;",
                "varying vec4 color;",
                "void main()",
                "{",
                "   gl_Position = g_ViewProjMat * g_mWorldMat * vec4(g_Position, 1.0);",
                "   texCoord = g_TexCoord0;",
                "   color = g_Color0;",
                "}"
            ].join("\n");
            let fShaderStr = [
                "precision mediump float;",
                "uniform sampler2D tex;",
                "varying vec2 texCoord;",
                "varying vec4 color;",
                "void main()",
                "{",
                "   vec4 texColor = texture2D(tex, texCoord);",
                "   gl_FragColor = texColor * color;", // Texture with color.
                "}"
            ].join("\n");

            let vsh = genvidWebGL.loadVertexShader(vShaderStr);
            let fsh = genvidWebGL.loadFragmentShader(fShaderStr);
            this.gfx_prog = genvidWebGL.loadProgram(vsh, fsh, ["g_Position", "g_TexCoord0"]);
            this.gfx_prog_loc_viewproj = genvidWebGL.gl.getUniformLocation(this.gfx_prog, "g_ViewProjMat");
            this.gfx_prog_loc_world = genvidWebGL.gl.getUniformLocation(this.gfx_prog, "g_mWorldMat");
        }

        // Web gl initialization of render command
        gfx_initRenderCommands() {
            let genvidWebGL = this.genvidWebGL;
            let gl = genvidWebGL.gl;

            // Utility function.
            function handleTextureLoaded(image, texture, options) {
                options = options || {};
                gl.bindTexture(gl.TEXTURE_2D, texture);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
                if (options.wrap) {
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, options.wrap);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, options.wrap);
                }
                if (options.aniso) {
                    let ext = (
                        gl.getExtension("EXT_texture_filter_anisotropic") ||
                        gl.getExtension("MOZ_EXT_texture_filter_anisotropic") ||
                        gl.getExtension("WEBKIT_EXT_texture_filter_anisotropic")
                    );
                    if (ext) {
                        let max = gl.getParameter(ext.MAX_TEXTURE_MAX_ANISOTROPY_EXT);
                        // console.log("Enabling aniso filtering", max);
                        gl.texParameterf(gl.TEXTURE_2D, ext.TEXTURE_MAX_ANISOTROPY_EXT, max);
                    }
                }
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.bindTexture(gl.TEXTURE_2D, null);
            }

            let onload = this.gfx_draw3D.bind(this);

            // tank highlight
            {
                // Only prepare textures.
                let cmd = new RenderCommand();
                let options = {
                    "wrap": gl.CLAMP_TO_EDGE,
                    "aniso": true,
                };
                cmd.tex = gl.createTexture();
                cmd.img = new Image();
                cmd.img.onload = function () { handleTextureLoaded(cmd.img, cmd.tex, options); if (onload) onload(); };
                cmd.img.src = "img/highlight.png";
                cmd.visible = true;
                this.gfx_cmd_selection = cmd;
            }

            // billboard ad
            {
                // Only prepare textures.
                let cmd = new RenderCommand();
                let options = {
                    "wrap": gl.CLAMP_TO_EDGE,
                    "aniso": true,
                };
                cmd.tex = gl.createTexture();
                cmd.img = new Image();
                cmd.img.onload = function () { handleTextureLoaded(cmd.img, cmd.tex, options); if (onload) onload(); };
                cmd.img.src = "img/billboardImage.png";
                cmd.visible = true;
                this.gfx_cmd_ad = cmd;
            }

            this.gfx_draw3D();
        }

        // ---------------------------------------------------------Utility methods section---------------------------------------------------------
        // create a simple quad centered at (0,0,0)
        SetupVertexBuffer(dst: number[], size: number) {
            dst.push(
                // X   Y   Z     U    V      R    G    B    A
                0, 0 + size, 0 + size, 0.0, 0.0, 1, 1, 1, 1,
                0, 0 + size, 0 - size, 1.0, 0.0, 1, 1, 1, 1,
                0, 0 - size, 0 - size, 1.0, 1.0, 1, 1, 1, 1,
                0, 0 - size, 0 + size, 0.0, 1.0, 1, 1, 1, 1
            );
        }

        // Utility routine preparing a circle on the XY-plane
        // centered at {x,y,z}, of radius r and color c.
        makeCircleY(dst: number[], x: number, y: number, z: number, r: number, c: genvidMath.IVec4) {
            dst.push(
                // X   Y   Z     U    V      R    G    B    A
                x - r, y, z - r, 0.0, 0.0, c.x, c.y, c.z, c.w,
                x + r, y, z - r, 1.0, 0.0, c.x, c.y, c.z, c.w,
                x + r, y, z + r, 1.0, 1.0, c.x, c.y, c.z, c.w,
                x - r, y, z + r, 0.0, 1.0, c.x, c.y, c.z, c.w
            );
        }

        // Converts a @rad_3d around a 3D position @pos_3d using the viewProjection matrix @mat.
        // Returns an array [pos_2d, rad_2d].
        projectWithRadius(mat: genvidMath.IMat4, pos_3d: genvidMath.IVec3, rad_3d: number): [genvidMath.IVec3, number] {
            // There might be a more mathematically sound solution to this,
            // but I've decided to use the shotgun approach and just project
            // 8 positions (add/sub radius to every dimension), and keep
            // the one which yields the largest 2D distance.
            let pos_2d = genvidMath.projectPosition(mat, pos_3d);
            let rad_sq_2d = 0;
            let offsets = [
                genvidMath.vec3(rad_3d, 0, 0),
                genvidMath.vec3(-rad_3d, 0, 0),
                genvidMath.vec3(0, rad_3d, 0),
                genvidMath.vec3(0, -rad_3d, 0),
                genvidMath.vec3(0, 0, rad_3d),
                genvidMath.vec3(0, 0, -rad_3d),
            ];
            for (let i = 0; i < offsets.length; ++i) {
                let off = offsets[i];
                let n_pos_3d = genvidMath.add3D(pos_3d, off);
                let n_pos_2d = genvidMath.projectPosition(mat, n_pos_3d);
                let n_rad_sq_2d = genvidMath.lengthSq2D(genvidMath.sub2D(pos_2d, n_pos_2d));
                rad_sq_2d = Math.max(rad_sq_2d, n_rad_sq_2d);
            }
            return [pos_2d, Math.sqrt(rad_sq_2d)];
        }

        // Change the HTML element position to be at the center of the pos 2d sent
        center_at(html_element: HTMLElement, pos_2d: genvidMath.IVec2, offset_pixels: genvidMath.IVec2) {
            // Convert from [-1, 1] range to [0, 1].
            let vh = genvidMath.vec2(0.5, 0.5);
            let pos_2d_n = genvidMath.mad2D(pos_2d, vh, vh);

            // Convert from [0, 1] range to [0, w].
            let p = html_element.parentElement;
            let p_size = genvidMath.vec2(p.clientWidth, p.clientHeight);
            let pos_in_parent = genvidMath.mul2D(pos_2d_n, p_size);

            // Adjust for centering element.
            let e_size = genvidMath.vec2(html_element.clientWidth, html_element.clientHeight);
            let e_offset = genvidMath.muls2D(e_size, -0.5);
            let pos_centered = genvidMath.add2D(pos_in_parent, e_offset);

            // Apply user offset.
            let pos_final = genvidMath.sub2D(pos_centered, offset_pixels);
            $(html_element).css({ left: pos_final.x, bottom: pos_final.y, position: "absolute" });
        }

        // Widens a string to at least n characters, prefixing with spaces.
        private preN(str: string, n: number): string {
            let s: number = str.length;
            if (s < n) {
                str = " ".repeat(n - s) + str;
            }
            return str;
        }

        // Convert an array of 14 entry into a genvidMath Matrix 4x4
        convertMatrix(rawmat) {
            return genvidMath.mat4(genvidMath.vec4(rawmat.e00, rawmat.e01, rawmat.e02, rawmat.e03),
                genvidMath.vec4(rawmat.e10, rawmat.e11, rawmat.e12, rawmat.e13),
                genvidMath.vec4(rawmat.e20, rawmat.e21, rawmat.e22, rawmat.e23),
                genvidMath.vec4(rawmat.e30, rawmat.e31, rawmat.e32, rawmat.e33));
        }

        // Find or create the div needed for the moving name on the overlay
        findOrCreateTagDiv(tank: ITankData): HTMLElement {
            let elem_id: string = "tank_tag_" + tank.id;
            let elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + elem_id);
            if (elem == null) {
                elem = document.createElement("div");
                elem.id = elem_id;
                elem.textContent = tank.name;
                elem.classList.add("tag");

                let parent: HTMLElement = <HTMLDivElement>document.querySelector("#mouse_overlay");
                parent.appendChild(elem);

                elem.addEventListener("click", (_event) => { this.setSelection(tank.id); _event.stopPropagation(); }, false);
            }
            return elem;
        }

        // Find or create the div needed for the moving name on the overlay
        findOrCreateCheerDiv(tank: ITankData): HTMLElement {
            let elem_id: string = "tank_cheer_" + tank.id;
            let elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + elem_id);
            if (elem == null) {
                elem = document.createElement("div");
                elem.id = elem_id;
                elem.classList.add("cheers");
                let parent: HTMLElement = <HTMLDivElement>document.querySelector("#mouse_overlay");
                parent.appendChild(elem);
            }

            let localTime: Date = new Date();
            let time: number = Math.round(localTime.getTime());

            if (tank.id == 1) {
                elem.textContent = "+" + this.blueCheers.toString() + " cheers";
                let alpha: number = ((this.blueCheerTime - time) / 2000);
                elem.style.opacity = alpha.toString();
            } else if (tank.id == 2) {
                elem.textContent = "+" + this.redCheers.toString() + " cheers";
                let alpha: number = ((this.redCheerTime - time) / 2000);
                elem.style.opacity = alpha.toString();
            }

            return elem;
        }

        // Find or create the div needed for the moving name on the overlay
        findOrCreateDetailsDiv(tank: ITankData): HTMLElement {
            let elem_id: string = "tank_details_" + tank.id; // remove spaces
            let elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + elem_id);
            
            var accuracy = Math.round(tank.shotsHit / tank.shotsFired * 100);
            if (isNaN(accuracy)) accuracy = 0;

            if (elem == null) {
                elem = document.createElement("div");
                elem.id = elem_id;

                let tankAdd = `<div id=` + elem_id + "_inner" +`></div>`;
                elem.innerHTML = tankAdd;
                elem.classList.add("details");

                let parent: HTMLElement = <HTMLDivElement>document.querySelector("#mouse_overlay");
                parent.appendChild(elem);
            }

            let inner_elem_id: string = "tank_details_" + tank.id + "_inner"; // remove spaces
            let inner_elem: HTMLElement = <HTMLDivElement>document.querySelector("#" + inner_elem_id);

            // update state
            var buffString:string;
            if (tank.activeBuff == 2) buffString = "Attack";
            else if (tank.activeBuff == 3) buffString = "Movement";
            else if (tank.activeBuff == 4) buffString = "Shield";
            else buffString = "none";

            let tankAdd = `Health: ` + tank.health.toFixed(0) + `%</br>`
                           + `Shots fired: ` + tank.shotsFired + `</br>`
                           + `Shots hit: ` + tank.shotsHit + `</br>`
                           + `Accuracy: ` + accuracy + `%</br>`
                           + `Active Buff: ` + buffString + `</br>`
                           + `Cheers: ` + tank.cheers;
            inner_elem.innerHTML = tankAdd;

            return elem;
        }

        // Method used to convert ms to specific duration
        private msToDuration(duration: number): string {
            let res = "";
            if (duration < 0) {
                res += "-";
                duration = -duration;
            }
            let second = 1000;
            let minute = second * 60;
            let hour = minute * 60;
            let day = hour * 24;
            let started = false;
            if (duration > day) {
                started = true;
                let rest = duration % day;
                let days = (duration - rest) / day;
                duration = rest;
                res += `${days}:`;
            }
            if (started || duration > hour) {
                started = true;
                let rest = duration % hour;
                let hours = (duration - rest) / hour;
                duration = rest;
                if (hours < 10) {
                    res += "0";
                }
                res += `${hours}:`;
            }
            if (started || duration > minute) {
                started = true;
                let rest = duration % minute;
                let minutes = (duration - rest) / minute;
                duration = rest;
                if (minutes < 10) {
                    res += "0";
                }
                res += `${minutes}:`;
            }
            if (started || duration > second) {
                let rest = duration % second;
                let seconds = (duration - rest) / second;
                duration = rest;
                if (seconds < 10) {
                    res += "0";
                }
                res += `${seconds}.`;
            } else {
                res += "0.";
            }
            if (duration < 100) {
                res += "0";
            }
            if (duration < 10) {
                res += "0";
            }
            return res + `${duration}`;
        }

        checkFullScreen(): boolean {
            let doc = <any>document;
            return doc.fullscreenElement || doc.webkitFullscreenElement || doc.mozFullScreenElement || doc.msFullscreenElement;
        }

        mapUpdate() {
            if (unityVideo.context2d != null && unityVideo.tankImage.complete && unityVideo.mapImage.complete && unityVideo.shellImage.complete && unityVideo.impactImage.complete && unityVideo.tankData) {

                unityVideo.context2d.drawImage(unityVideo.mapImage, 0, 0, unityVideo.canvas2d.width, unityVideo.canvas2d.height);
                let tanks = unityVideo.tankData;
                for (let tank of tanks) {
                    if (tank.health <= 0) continue; // don't render dead tanks

                    let tankSize = 32;
                    let tankCenter = -tankSize / 2;
                    let m = tank.matrix;
                    let p = genvidMath.vec3(m.e03, m.e13, m.e23);                    
                    let angleInRadians = Math.atan2(m.e02, m.e00);
                    let maxWorldDistance = 45;
                    let xPos = Math.round((p.x + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.width) + tankCenter;
                    let yPos = Math.round((p.z + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.height) + tankCenter;

                    unityVideo.context2d.save();
                    unityVideo.context2d.translate(xPos, yPos);
                    unityVideo.context2d.scale(1, -1);
                    unityVideo.context2d.rotate(angleInRadians);
                    unityVideo.context2d.drawImage(unityVideo.tankImage, tankCenter, tankCenter, tankSize, tankSize);

                    let color = "rgb("+ (tank.color.r * 255) + ","+ (tank.color.g * 255) + ","+ (tank.color.b * 255) + ")";
                    unityVideo.context2d.globalCompositeOperation = "source-atop";
                    unityVideo.context2d.globalAlpha = 0.5;
                    unityVideo.context2d.fillStyle = color;
                    unityVideo.context2d.fillRect(tankCenter, tankCenter, tankSize, tankSize);

                    unityVideo.context2d.restore();
                }

                let shells = unityVideo.shellMatrices;
                for (let shell of shells) {
                    let shellSize = 24;
                    let shellCenter = -shellSize / 2;
                    let m = shell;
                    let p = genvidMath.vec3(m.e03, m.e13, m.e23);                    
                    let angleInRadians = Math.atan2(m.e02, m.e00);
                    let maxWorldDistance = 45;
                    let xPos = Math.round((p.x + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.width) + shellCenter;
                    let yPos = Math.round((p.z + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.height) + shellCenter;

                    unityVideo.context2d.save();
                    unityVideo.context2d.translate(xPos, yPos);
                    unityVideo.context2d.scale(1, -1);
                    unityVideo.context2d.rotate(angleInRadians);
                    unityVideo.context2d.drawImage(unityVideo.shellImage, shellCenter, shellCenter, shellSize, shellSize);
                    unityVideo.context2d.restore();
                }

                let lootDrops = unityVideo.lootMatrices;
                for (let loot of lootDrops) {
                    let lootSize = 32;
                    let lootCenter = -lootSize / 2;
                    let m = loot;
                    let p = genvidMath.vec3(m.e03, m.e13, m.e23);                    
                    let angleInRadians = Math.atan2(m.e02, m.e00);
                    let maxWorldDistance = 45;
                    let xPos = Math.round((p.x + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.width) + lootCenter;
                    let yPos = Math.round((p.z + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.height) + lootCenter;

                    unityVideo.context2d.save();
                    unityVideo.context2d.translate(xPos, yPos);
                    unityVideo.context2d.scale(1, -1);
                    unityVideo.context2d.rotate(angleInRadians);
                    unityVideo.context2d.drawImage(unityVideo.lootImage, lootCenter, lootCenter, lootSize, lootSize);
                    unityVideo.context2d.restore();
                }

                let explosions = unityVideo.explosionMatrices;
                for (let explosion of explosions) {
                    let explosionSize = 32;
                    let explosionCenter = -explosionSize / 2;
                    let m = explosion;
                    let p = genvidMath.vec3(m.e03, m.e13, m.e23);                    
                    let angleInRadians = Math.atan2(m.e02, m.e00);
                    let maxWorldDistance = 45;
                    let xPos = Math.round((p.x + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.width) + explosionCenter;
                    let yPos = Math.round((p.z + maxWorldDistance) / (maxWorldDistance * 2) * unityVideo.canvas2d.height) + explosionCenter;

                    unityVideo.context2d.save();
                    unityVideo.context2d.translate(xPos, yPos);
                    unityVideo.context2d.scale(1, -1);
                    unityVideo.context2d.rotate(angleInRadians);
                    unityVideo.context2d.drawImage(unityVideo.impactImage, explosionCenter, explosionCenter, explosionSize, explosionSize);
                    unityVideo.context2d.restore();
                }


            }
            window.requestAnimationFrame(unityVideo.mapUpdate);
        }
    }
}

// Unity Controller creation
let unityVideo = new unityTankSample.UnityController("video_player");
unityVideo.onConnect();
window.addEventListener("keydown", (event) => { unityVideo.onKeyDown(event); }, true);