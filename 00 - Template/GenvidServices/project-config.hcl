// Project Name
project "GenvidTanks" {

    // Enter a short description of the project
    description = "GenvidTanksforUnity"
	
    // Enter one or many configuration file matching the ones in config folder
    // Setting required to true means that the file requires a template module
    config "stream" { required=true }
    config "events" { required=true }
    config "web" { required=true }
    config "game" { required=true }
}