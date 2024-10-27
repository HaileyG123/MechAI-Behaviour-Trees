tree("Root")
        repeat
                mute
                        race
                                tree "Firing"
                                tree "Cannoning"
                                tree "Beaming"
                                tree "Missiling"
                                tree "Swivelling"
tree("Firing")
        while
                sequence
                        HasAttackTarget()
                repeat
                        Laser()
tree("Cannoning")
        while
                sequence
                        HasAttackTarget()
                repeat
                        Cannon()
tree("Beaming")
        while
                sequence
                        HasAttackTarget()
                repeat
                        LaserBeam()
tree("Missiling")
        while
                sequence
                        HasAttackTarget()
                repeat
                        MissileArray()
tree("Swivelling")
        while
                sequence
                        HasAttackTarget()
                        not TargetLOS()
                repeat
                        Swivel()