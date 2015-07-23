#ifndef aiSchema_h
#define aiSchema_h

class aiSchema
{
public:
    aiSchema();
    aiSchema(aiObject *obj);
    virtual ~aiSchema();

    virtual void updateSample() = 0;

protected:
    aiObject *m_obj;
};

#endif
