var five = require('johnny-five');
var board = new five.Board({});

board.on('ready', function () {
	console.log('Ready!');
	
	var led = five.Led(11);
	led.pulse({
		easing: "linear",
		duration: 3000,
		onstop: function() {
		  console.log("Animation stopped");
		}
	});
	
	this.on('exit', function() {
		led.stop().off();
	});
});
