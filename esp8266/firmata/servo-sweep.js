var EtherPortClient = require('etherport-client').EtherPortClient;

var five = require('johnny-five');

var board = new five.Board({
    port: new EtherPortClient({
        host: "192.168.192.117",
        port: 3030
    }),
    timeout: 1e5,
    repl: false
});

board.on("ready", function() {
    console.log('Board is ready!');

  var servo = new five.Servo({
    pin: 2,
    startAt: 90,
    range: [0, 180],
    debug: true
  });

  console.log('Servo connected');
  
  var lap = 0;

  servo.sweep().on("sweep:full", function() {
    console.log("lap", ++lap);

    if (lap === 1) {
      this.sweep({
        range: [40, 140],
        step: 10
      });
    }

    if (lap === 2) {
      this.sweep({
        range: [60, 120],
        step: 5
      });
    }

    if (lap === 3) {
      this.sweep({
        range: [80, 100],
        step: 1
      });
    }

    if (lap === 5) {
      process.exit(0);
    }
  });

  this.on("exit", function() {
      servo.to(90);
  });
});