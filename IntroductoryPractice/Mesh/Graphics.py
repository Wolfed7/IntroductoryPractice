import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D

def main():

    x = []
    y = []
    z = []

    figure = plt.figure()
    axes = figure.add_subplot(111, projection='3d')
    
    # plt.xlim(-3,3)
    # plt.ylim(-3,3)
    
    #plt.xticks(np.arange(-4, 4, 0.2))
    #plt.yticks(np.arange(-4, 4, 0.2))
    
    with open("AllRibs.dat") as file:
        for line in file:
            x1C, y1C, z1C, x2C, y2C, z2C = line.split()
            x.append(float(x1C))
            y.append(float(y1C))
            z.append(float(z1C))
            x.append(float(x2C))
            y.append(float(y2C))
            z.append(float(z2C))
            axes.plot(x, y, z, '-s', markersize=2, color='black')
            x.clear()
            y.clear()
            z.clear()

    x = np.array([1, 2, 3])
    y = np.array([4, 5, 6])
    z = np.array([[7, 8, 9], [10, 11, 12], [13, 14, 15]])

    X, Y = np.meshgrid(x, y)
    axes.contour3D(X, Y, z)
    #axes.plot(x, y, z, markersize=2, color='black')
    axes.set_xlabel('X Label')
    axes.set_ylabel('Y Label')
    axes.set_zlabel('Z Label')
    axes.set_title('3D Line Plot')

    #plt.grid()
    plt.show()

if __name__ == "__main__":
    main()
