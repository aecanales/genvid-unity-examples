version = "1.7.0"

secrets {
  disco {
    GENVID_DISCO_SECRET = "discosecret"
  }
  webgateway {
    GENVID_WEBGATEWAY_SECRET = "webgatewaysecret"
  }
}

settings {
  encode {
    input {
      silent = false
    }
    stream {
      extradelay = 2
    }
  }
}