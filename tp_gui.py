# Launch the main GUI

import sys
sys.path.append(".") # If launching from ROOT
from build.tinyprocbuild import *

def tp_gui():
    cmd_run([DOTNET, DOTNET_GUI_RUN_ARGS])

    target_finish()

def tp_gui_enqueue():
    enqueue_target("GUI", tp_gui)

if __name__ == "__main__":
    tp_gui_enqueue()