tree("Root")
        repeat
                mute
                        fallback
                                tree "Swivelling"
tree("Swivelling")
        while
                sequence
                        HasAttackTarget()
                        not TargetLOS()
                repeat
                        Swivel()