# that-conference-2017
Code samples for That Conference Robotics / IoT Lab 2017

## Johnny Five

Johnny Five is a Node JS library that can be used to communicate with IoT boards running the Firmata sketch.  In these examples, we'll be using the Adafruit Feather HUZZAH board, which is a Firmata compatible board which can be attached to via WiFi.

### Prepping the environment

1. Open the NodeJS command prompt (on windows) or the terminal (on Linux).
2. Change to the directory you want to use for coding
3. Load the johnny-five node module
    ```
    npm install johnny-five
    ```
4. When this completes, you should be ready to go!

### Determine the board IP address

1. Open Arduino studio and choose 'Tools -> Serial Monitor'
2. This will open a 'terminal' window that shows the output from the serial port on the board.  Find the IP address that the board is using, as you'll need it to communicate with the board.  In the screenshot below, the IP address is '192.168.192.117'.

    ![Serial Monitor](sketches/Serial_ConsoleOutput.png)

## Create your first project

You should have received a long breadboard with attached Feather HUZZAH as part of the materials for this lab.  The board should look like this:

![HUZZAH Baseline](sketches/Servos_Baseline.png)

Find a single (non-RGB) led of your choice, a 220Ω resistor and three wires, then setup your board to look like the following picture:

![HUZZAH LEDs](sketches/Feather_Led.png)

The 'longer' leg of the LED should be connected to the resistor, and the shorter leg should be connected to the ground plane.

### Create the NodeJS file

