"""
Reads all csv files in a directory and plots line plots and save them as pdfs
   in a new subdiretory of the provided directory.
"""

import warnings
warnings.filterwarnings("ignore")
import pandas as pd
from matplotlib import pyplot as plt
import matplotlib.backends.backend_pdf
import sys
from pathlib import Path
import Pillow

csv_directory = sys.argv[1]

csv_path = Path(csv_directory)
pdf_path = Path(csv_directory + '\\pdf')
pdf_path.mkdir(parents=True, exist_ok=True)

plt.style.use('ggplot')

x: Path
csv_files = [x for x in csv_path.iterdir() if x.is_file() and x.suffix == '.csv']

class paramStuffs:
    def __init__(self, file_name: Path):
        file_to_open = file_name.with_suffix('.txt')
        with open(file_to_open) as a_file:
            self.all_lines = [line.rstrip() for line in a_file]

    def get_formatted_left_text(self):
        start_idx = 4
        return f'{self.all_lines[start_idx]}\n{self.all_lines[start_idx+1]}\n{self.all_lines[start_idx+2]}\n{self.all_lines[start_idx+3]}\n'

    def get_formatted_right_text(self):
        start_idx = 0
        return f'{self.all_lines[start_idx]}\n{self.all_lines[start_idx+1]}\n{self.all_lines[start_idx+2]}\n{self.all_lines[start_idx+3]}\n'


outfile = str(pdf_path.resolve()) + '\\' + 'hyperbola plots.pdf'
pdf = matplotlib.backends.backend_pdf.PdfPages(outfile)
for a_file in csv_files:
    params = paramStuffs(a_file)

    # Set the figure size
    plt.rcParams["figure.figsize"] = [7.00, 3.50]
    plt.rcParams["figure.autolayout"] = True
    fig, ax = plt.subplots(figsize=(7.0, 3.5))
    ax.set_ylabel('Elevation')
    ax.set_xlabel('Offset')

    # Make a list of columns
    # columns = ['mpg', 'displ', 'hp', 'weight']
    columns = "station,elevation,vcLength".split(",")
    with open(a_file) as file_handle:
        columns = file_handle.readline().rstrip().split(",")
    columnsToUse = [x for x in columns if x != "vcLength"]

    # Read a CSV file
    df = pd.read_csv(a_file, header=0, names=columns, usecols=columnsToUse, index_col='station')

    # Plot the lines
    df_elevation = df["elevation"]
    df_elevation.plot.line(linewidth=2, color="dimgrey", linestyle='dashed', y=["elevation"])
    df_hyperbola = df["hyperbolaValue"]
    df_hyperbola.plot.line(linewidth=1, color="red", linestyle='solid')

    plt.legend()
    title = Path(a_file).stem[:-4]
    plt.title(title)
    left_text = params.get_formatted_left_text()
    right_text = params.get_formatted_right_text()
    plt.figtext(0.1, 0.01, left_text, horizontalalignment='left', fontsize=12)
    plt.figtext(0.7, 0.01, right_text, horizontalalignment='left', fontsize=12)

    plt.gca().set_aspect('equal')


    # plot = df.plot(linewidth=2, color="dimgrey", linestyle='dashed', title=a_file.stem)
    # color names at https://matplotlib.org/stable/gallery/color/named_colors.html
    # color blind palettes at https://yoshke.org/blog/colorblind-friendly-diagrams

    # fig = df.plot.get_figure()
    # pdf_file_name = a_file.name[:-4] + '.pdf'
    # new_file_name = str(pdf_path.resolve()) + '\\' + pdf_file_name
    # plt.savefig(new_file_name, format="pdf") #, bbox_inches="tight")
    # print(pdf_file_name)
    pdf.savefig()

    # plt.show()

    dbg = 1


pdf.close()




