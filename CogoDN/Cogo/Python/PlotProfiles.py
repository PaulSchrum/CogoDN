"""
Reads all csv files in a directory and plots line plots and save them as pdfs
   in a new subdiretory of the provided directory.
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
from matplotlib import pyplot as plt
import sys
from pathlib import Path

csv_directory = sys.argv[1]

csv_path = Path(csv_directory)
pdf_path = Path(csv_directory + '\\pdf')
pdf_path.mkdir(parents=True, exist_ok=True)

x: Path
csv_files = [x for x in csv_path.iterdir() if x.is_file() and x.suffix == '.csv']

for a_file in csv_files:
    # Set the figure size
    plt.rcParams["figure.figsize"] = [7.00, 3.50]
    plt.rcParams["figure.autolayout"] = True

    # Make a list of columns
    # columns = ['mpg', 'displ', 'hp', 'weight']
    columns = "station,elevation,vcLength".split(",")

    # Read a CSV file
    df = pd.read_csv(a_file, header=0, names=columns, usecols=columns[:-1], index_col='station')

    # Plot the lines
    plot = df.plot()
    fig = plot.get_figure()
    pdf_file_name = a_file.name[:-4] + '.pdf'
    new_file_name = str(pdf_path.resolve()) + '\\' + pdf_file_name
    plt.savefig(new_file_name, format="pdf") #, bbox_inches="tight")
    print(pdf_file_name)

    # plt.show()

    dbg = 1
