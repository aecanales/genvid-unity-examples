version = "1.7.0"

job "unity" {
  dependencies = [
    "nats",
    "compose",
  ]
}

log "game" {
  job      = "unity"
  fileName = "unity.log"
}

log "gameerr" {
  job      = "unity"
  fileName = "stderr"
  logLevel = true
}

config {
  local {
    binary {
      unity {
        path = "{{env `UNITYPROJECTROOT` | js}}\\Builds\\GenvidTemplate.exe"
      }
    }
  }
}
