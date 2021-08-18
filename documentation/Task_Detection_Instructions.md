# Task Detection Instructions

The task detection feature uses a couple of files to perform its computations. Follow the following links to download the files:

### Download Files

English dictionnary: https://osdn.net/projects/sfnet_scihun/downloads/Dictionaries/en_US.dic/ and https://osdn.net/projects/sfnet_scihun/downloads/Dictionaries/en_US.aff/

Word2vec model: https://github.com/eyaler/word2vec-slim/blob/master/GoogleNews-vectors-negative300-SLIM.bin.gz

### Store Files

1. Run PA - this will create a PersonalAnalytics folder in your local AppData folder
2. Right click on the system tray icon -> right click on PA (white figure of a man) -> open collected data
3. This is the folder in which you want to place the files (you may stop the program)
4. Create a folder called "task_detection_files" and store the three downloaded files in that folder


# General Tips

- To run and make changes to PA locally: First, fork this repository and clone the master branch. Then, navigate into /.../client/AM.PA.MonitoringTool and open the "AM.PA.MonitoringTool.sln" file in an IDE 
(Visual Studio is recommended). Finally, in the solution explorer, right-click on "PersonalAnalytics" and click "Set as startup project".

- If you have PA installed and you also want to debug PA, temporarily change the "pa.dat" file to "pa.orig.dat". You can find this file if you right-click on the system tray icon 
and choose open collected data. For your debugging, use another sqlite db (e.g "pa.debug.dat"). This ensures that you do not interfere with your own retrospection.
