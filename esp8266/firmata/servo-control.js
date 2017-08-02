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

  var control = new five.Sensor({
      pin: "A0",
      freq: 250
  });

  control.on("data", function() {
    console.log('Value: ', this.value, ', Raw: ', this.raw);
  });

  control.on("change", function() {
      var controlValue = this.scaleTo(0, 180);
      servo.to(controlValue);
  });
});