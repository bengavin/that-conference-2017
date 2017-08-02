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
    //center: true,
    debug: true
  });

  console.log('Servo connected');

  var currentDegrees = 0;
  var degreeDelta = 10;

  var lap = 0;
  servo.on("move:complete", function() {
    if (currentDegrees >= 180 || currentDegrees < 0) {
        degreeDelta *= -1;
        console.log('Lap ', lap + 1, ' complete');
        lap++;

        if (lap >= 5) { 
            servo.stop();
            process.exit(0); 
        }
    }

    currentDegrees += degreeDelta;
    servo.to(currentDegrees, 500);
  });

  servo.to(0, 500);

  this.on("exit", function() {
      servo.to(90);
  });
});