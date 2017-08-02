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
    this.pinMode(14, five.Pin.PWM);

    var led = five.Led(14);

    var goingIn = true;

    led.off();

    led.pulse({
        easing: "linear",
        duration: 1000,
        cuePoints: [0, 0.2, 0.4, 0.6, 0.8, 1],
        keyFrames: [0, 250, 25, 150, 100, 125],
    });

    this.wait(1000, function() {
        led.pulse();
    });

    this.on("exit", function() {
        led.off();
    });
});
