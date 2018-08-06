using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CSML;

public class GameController : MonoBehaviour
{
    public GameObject hazard;
    public PlayerController player;
    public Vector3 spawnValues;
    public int hazardCount;
    public float spawnWait, waveWait, startWait;
    private bool restart;
    public Text endText;


    private int num_players;
    private const int N_PLAYERS=30;
    private List<Matrix> theta1list, theta2list,theta3list;
    private float[] scores;
    private int generation;
    private bool gen_pause, auto_gen=true;

    //mutation variables:
    private float mutation_range = 1.3f;
    private int mutation_min = 2, mutation_max = 8;
    private float top_score = -1, curr_avg_top = -1;

    private const int num_angles = 5, num_hidden_units_1 = 10, num_hidden_units_2 = 12, num_outputs = 1;

    void Start()
    {
        endText.color = new Color(255, 255, 255, 0);
        StartCoroutine(SpawnWaves());
        restart = false;
        num_players = N_PLAYERS;
        theta1list = new List<Matrix>(N_PLAYERS);
        theta2list = new List<Matrix>(N_PLAYERS);
        theta3list = new List<Matrix>(N_PLAYERS);

        for (int i = 0; i < N_PLAYERS; i++)
        {
            Vector3 initialSpawnPos = new Vector3(0, 0, 0);
            Quaternion initialSpawnRotation = Quaternion.identity;
            PlayerController clone =  Instantiate<PlayerController>(player, initialSpawnPos, initialSpawnRotation);
            clone.th1 = getRandomMatrix(num_hidden_units_1,num_angles+1, 0.25f);
            clone.th2 = getRandomMatrix(num_hidden_units_2, num_hidden_units_1 + 1, 0.1f);
            clone.th3 = getRandomMatrix(num_outputs, num_hidden_units_2 + 1, 0.1f);
            theta1list.Add(clone.th1);
            theta2list.Add(clone.th2);
            theta3list.Add(clone.th3);
            clone.id = i;
        }

        generation = 1;
        gen_pause = false;
        scores = new float[N_PLAYERS];
        //Matrix m = new Matrix(4,4);
        //Debug.Log(m);
    }

    Matrix getRandomMatrix(int m, int n, float scale)
    {
        double[,] mat  = new double [m, n];
        for(int i =0; i<m; i++)
        {
            for(int j=0; j<n; j++)
            {
                mat[i, j] = (double)Random.Range(-scale,scale);
            }
        }
        Matrix randMatrix = new Matrix(mat);
        return randMatrix;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Application.LoadLevel(Application.loadedLevel);
        }else if (Input.GetKeyDown(KeyCode.N) && gen_pause)
        {
            start_next_generation();
            gen_pause = false;
        }else if (Input.GetKeyDown(KeyCode.P)) {
            auto_gen = !auto_gen;
        }
    }

    IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(startWait);
        while (true)
        {
            if (!gen_pause)
            {
                Vector3 rightSpawnPos = new Vector3(spawnValues.x, spawnValues.y, spawnValues.z);
                Quaternion rot = Quaternion.identity;
                Instantiate(hazard, rightSpawnPos, rot);
                Vector3 leftSpawnPos = new Vector3(-spawnValues.x, spawnValues.y, spawnValues.z);
                Quaternion rotl = Quaternion.identity;
                Instantiate(hazard, leftSpawnPos, rotl);
            }
            else
            {
                start_next_generation(true);
                gen_pause = false;
            }
            for (int i = 0; i < hazardCount; i++)
            {
                if (gen_pause)
                {
                    break;
                }
                if( GameObject.FindGameObjectsWithTag("Player").Length == 0){
                    num_players = 0;
                    procreate();
                }
                Vector3 spawnPosition = new Vector3(Random.Range(-spawnValues.x, spawnValues.x), spawnValues.y, spawnValues.z);
                Quaternion spawnRotation = Quaternion.identity;
                Instantiate(hazard, spawnPosition, spawnRotation);
                yield return new WaitForSeconds(spawnWait);

            }
           
            yield return new WaitForSeconds(waveWait);
            
        }
    }

   public void recordDeath(int id, float score)
   {
        //Debug.Log(id + " " + score );
        //Debug.Log(thetas.Length);
        //Debug.Log(thetas[0,1]);
        scores[id] = score;
        //thetas[id,0] = th_params[0];
        //thetas[id, 1] = th_params[1];

        num_players -= 1;
        
        if (extict())
        {
            procreate();
        }
        
   }

    bool extict()
    {
        return num_players == 0;
    }

    public void procreate()
    {
        gen_pause = true;
        int[] fittest = selectFittest();
        reproduce(fittest);

    }

    public void reproduce(int [] fittest)
    {
        int mutation_rate = Random.Range(mutation_min, mutation_max), 
             curr_num = 0;//keeps track of how many values it has iterated through
        List<Matrix> next_generation_th1 = new List<Matrix>();
        List<Matrix> next_generation_th2 = new List<Matrix>();
        List<Matrix> next_generation_th3 = new List<Matrix>();

        for (int i = 0; i < N_PLAYERS; i++)
        {
            int a = Random.Range(0, fittest.Length);
            int b = a;
            if (fittest.Length != 1)
            {
                while (b == a)
                {
                    b = Random.Range(0, fittest.Length);
                }
            }
            Matrix new_th1 = randomMergeMatricies(theta1list[a], theta1list[b], a, b);
            Matrix new_th2 = randomMergeMatricies(theta2list[a], theta2list[b], a, b);
            Matrix new_th3 = randomMergeMatricies(theta3list[a], theta3list[b], a, b);
            mutate(ref new_th1, mutation_rate, ref curr_num);
            mutate(ref new_th2, mutation_rate, ref curr_num);
            mutate(ref new_th3, mutation_rate, ref curr_num);
            next_generation_th1.Add(new_th1);
            next_generation_th2.Add(new_th2);
            next_generation_th3.Add(new_th3);
        }
        theta1list = next_generation_th1;
        theta2list = next_generation_th2;
        theta3list = next_generation_th3;
        generation += 1;
        Debug.Log("gen " + generation);
    }

    public Matrix randomMergeMatricies(Matrix a, Matrix b, int a_ind, int b_ind)
    {
        Matrix baby = new Matrix(a.RowCount, a.ColumnCount);
        List<int> rand_indicies = new List<int>();
        int a_weight = (int)scores[a_ind], b_weight =  (int)scores[b_ind];
        for(int i=0; i<a_weight; i++)
        {
            rand_indicies.Add(0);
        }
        for (int i = 0; i < b_weight; i++)
        {
            rand_indicies.Add(1);
        }

        for (int i=1; i<=a.RowCount; i++)
        {
            for(int j=1; j<=b.ColumnCount; j++)
            {
                int parent = Random.Range(0, a_weight+b_weight);
                if(parent == 0)
                {
                    baby[i, j] = a[i, j];
                }
                else
                {
                    baby[i, j] = b[i, j];
                    
                }
            }
        }
        return baby;
    }

    public void mutate(ref Matrix baby, int mutation_rate, ref int curr_num)
    {
        for(int i=1; i<=baby.RowCount; i++)
        {
            for(int j=1; j<=baby.ColumnCount; j++)
            {
                curr_num += 1;
                if(curr_num%mutation_rate == 0)
                {
                    int sign = Random.Range(0, 2)==1 ? 1:-1;
                    baby[i, j] = sign *baby[i,j] + baby[i,j]*Random.Range(-0.05f,0.05f);
                }
            }
        }
    }


    public int[] selectFittest()
    {
        int n_survivors = 3;
        int [] survivors_list = new int[n_survivors];
        float [] top_scores = new float[n_survivors];
        for(int i=0; i<n_survivors; i++)
        {
            int max_score_ind = 0;
            for (int j = 1; j < N_PLAYERS; j++)
            {
                if (scores[j] > scores[max_score_ind])
                {
                    max_score_ind = j;
                }
            }
            survivors_list[i] = max_score_ind;
            top_scores[i] = scores[max_score_ind];
            scores[max_score_ind] = -1;
            Debug.Log("player: " + max_score_ind + " score: " + top_scores[i]);
        }
        curr_avg_top = 0;
        for(int i=0; i<n_survivors; i++)
        {
            curr_avg_top += top_scores[i];
            if (top_scores[i] > top_score)
            {
                top_score = top_scores[i];
            }
        }
        curr_avg_top /= n_survivors;
        if (top_score == -1)
        {
            top_score = curr_avg_top;
        }
        //if (curr_avg_top > top_score)
        //{
        //    reward_mutation();
        //}
        //else
        //{
        //    penalize_mutation();
        //}
        if (curr_avg_top > top_score)
        {
            top_score = curr_avg_top;
        }
        weigh_tops(top_scores, survivors_list);
        return survivors_list;
    }

    public void weigh_tops(float[] top_scores, int[] survivors)
    {
        float[] rel_weights = new float[survivors.Length];
        float min_score = Mathf.Min(top_scores);
        for(int i=0; i<survivors.Length; i++)
        {
            scores[survivors[i]] = 20 * top_scores[i] / min_score;
        }
    }

    public void reward_mutation()
    {
        mutation_max += 1;
        mutation_min += 1;
        if(mutation_min<1)
        {
            mutation_min = 1;
        }
        if (mutation_max < 2)
        {
            mutation_max = 2;
        }
        if (mutation_max == mutation_min || mutation_max < mutation_min)
        {
            mutation_max = mutation_min + 1;
        }
        mutation_range -= 0.1f;
        if (mutation_range < 1.01)
        {
            mutation_range = 1.1f;
        }
    }

    public void penalize_mutation()
    {
        mutation_max -=1;
        mutation_min -= 1;
        if (mutation_min < 1)
        {
            mutation_min = 1;
        }
        if (mutation_max < 2)
        {
            mutation_max = 2;
        }
        if (mutation_max == mutation_min || mutation_max < mutation_min)
        {
            mutation_max = mutation_min + 1;
        }
        mutation_range += 0.2f;
        if (mutation_range > 3.5f)
        {
            mutation_range = 3.4f;
        }
    }

    public void start_next_generation()
    {
        for (int i = 0; i < N_PLAYERS; i++)
        {
            Vector3 initialSpawnPos = new Vector3(0, 0, 0);
            Quaternion initialSpawnRotation = Quaternion.identity;
            PlayerController clone = Instantiate<PlayerController>(player, initialSpawnPos, initialSpawnRotation);
            clone.th1 = theta1list[i];
            clone.th2 = theta2list[i];
            clone.th3 = theta3list[i];
            clone.id = i;
        }
        num_players = N_PLAYERS;
    }
    
    public void start_next_generation(bool auto_call)
    {
        if (auto_gen)
        {
            start_next_generation();
        }
    }


    public void checkEndGame()
    {
        //endText.color = new Color(endText.color.r, endText.color.g,endText.color.b, 1)
        if (num_players == 0)
        {
            endText.color = new Color(255, 255, 255, 1);
            restart = true;
        }
    }
}
