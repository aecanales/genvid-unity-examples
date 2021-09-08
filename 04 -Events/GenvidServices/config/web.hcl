version = "1.7.0"

job "web" {}

log "web" {
  job      = "web"
  task     = "web"
  fileName = "stdout"
}

log "weberr" {
  job      = "web"
  task     = "web"
  fileName = "stderr"
}

link "web" {
  name     = "Genvid Tanks Demo"
  template = "http://${service `web`}/"
}

link "admin" {
  name     = "Genvid Tanks Admin"
  template = "http://${serviceEx `web` `` true}/admin"
}

config {
  local {
	website {
	  root   = "{{env `PROJECTDIR` | js}}\\web"
	  script = "{{env `PROJECTDIR` | js}}\\web\\bin\\www"
	}
	binary {
	  node {
		path = "{{plugin `where.exe` `node` | js}}"
	  }
	}
  }
}
