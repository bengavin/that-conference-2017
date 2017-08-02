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
    console.log("READY!");
    var led = new five.Led(14);
    led.blink(500);

    this.on("exit", function() {
        led.off();
    });
});
